// Copyright (c) Microsoft. All rights reserved.

using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WeatherFunctionApp;

/// <summary>
/// Azure Function triggered by messages on the 'weather-requests' Service Bus topic.
/// Calls the free Open-Meteo API (https://open-meteo.com/) — no API key required.
/// Stores the result in Azure Blob Storage keyed by conversationId so
/// FetchResponseAgent can retrieve it.
/// </summary>
public class WeatherTriggerFunction
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WeatherTriggerFunction> _logger;

    // Blob container name must match WeatherResultStore in the main app
    private const string ContainerName = "weather-results";

    public WeatherTriggerFunction(IHttpClientFactory httpClientFactory, ILogger<WeatherTriggerFunction> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [Function("WeatherTrigger")]
    public async Task Run(
        [ServiceBusTrigger(
            topicName: "%SERVICE_BUS_TOPIC_NAME%",
            subscriptionName: "%SERVICE_BUS_SUBSCRIPTION_NAME%",
            Connection = "SERVICE_BUS_CONNECTION_STRING")]
        string messageBody,
        FunctionContext context)
    {
        _logger.LogInformation("WeatherTrigger received message: {Body}", messageBody);

        WeatherPayload? payload = JsonSerializer.Deserialize<WeatherPayload>(messageBody, JsonOptions);
        if (payload is null)
        {
            _logger.LogError("Failed to deserialize message body.");
            return;
        }

        _logger.LogInformation("Processing weather request for city='{City}', conversationId='{ConvId}'",
            payload.City, payload.ConversationId);

        HttpClient httpClient = _httpClientFactory.CreateClient();

        // Step 1: Geocode the city using Open-Meteo Geocoding API (free, no key)
        string geocodingUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(payload.City)}&count=1&language=en&format=json";
        GeocodingResponse? geo = await httpClient.GetFromJsonAsync<GeocodingResponse>(geocodingUrl);

        GeocodingResult? location = geo?.Results?.FirstOrDefault();
        if (location is null)
        {
            _logger.LogError("Could not geocode city '{City}'.", payload.City);
            await StoreErrorResultAsync(payload, $"Could not find location for city: {payload.City}");
            return;
        }

        _logger.LogInformation("Geocoded '{City}' to lat={Lat}, lon={Lon}", payload.City, location.Latitude, location.Longitude);

        // Step 2: Fetch current weather from Open-Meteo (free, no key)
        string weatherUrl = $"https://api.open-meteo.com/v1/forecast" +
                            $"?latitude={location.Latitude}&longitude={location.Longitude}" +
                            $"&current=temperature_2m,wind_speed_10m,weather_code" +
                            $"&timezone=auto";

        OpenMeteoResponse? weatherResponse = await httpClient.GetFromJsonAsync<OpenMeteoResponse>(weatherUrl);

        if (weatherResponse?.Current is null)
        {
            _logger.LogError("No weather data returned for '{City}'.", payload.City);
            await StoreErrorResultAsync(payload, $"No weather data returned for city: {payload.City}");
            return;
        }

        string condition = MapWeatherCode(weatherResponse.Current.WeatherCode);

        WeatherResult result = new(
            City: payload.City,
            Country: payload.Country ?? location.Country,
            TemperatureCelsius: weatherResponse.Current.Temperature2m,
            WindSpeedKmh: weatherResponse.Current.WindSpeed10m,
            WeatherDescription: condition,
            RetrievedAt: DateTime.UtcNow);

        await StoreResultAsync(payload.ConversationId, result);

        _logger.LogInformation(
            "Stored weather result for conversationId='{ConvId}': {Temp}C, {Wind}km/h, {Condition}",
            payload.ConversationId, result.TemperatureCelsius, result.WindSpeedKmh, result.WeatherDescription);
    }

    private async Task StoreResultAsync(string conversationId, WeatherResult result)
    {
        string connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
            ?? throw new InvalidOperationException("AZURE_STORAGE_CONNECTION_STRING is not configured.");

        BlobContainerClient container = new(connectionString, ContainerName);
        await container.CreateIfNotExistsAsync();

        BlobClient blob = container.GetBlobClient($"{conversationId}.json");
        string json = JsonSerializer.Serialize(result, JsonOptions);

        using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes(json));
        await blob.UploadAsync(stream, overwrite: true);
    }

    private async Task StoreErrorResultAsync(WeatherPayload payload, string errorMessage)
    {
        // Store an error result so FetchResponseAgent can surface it rather than timing out
        WeatherResult errorResult = new(
            City: payload.City,
            Country: payload.Country,
            TemperatureCelsius: 0,
            WindSpeedKmh: 0,
            WeatherDescription: $"Error: {errorMessage}",
            RetrievedAt: DateTime.UtcNow);

        await StoreResultAsync(payload.ConversationId, errorResult);
    }

    /// <summary>
    /// Maps WMO weather interpretation codes to human-readable descriptions.
    /// Reference: https://open-meteo.com/en/docs#weathervariables
    /// </summary>
    private static string MapWeatherCode(int code) => code switch
    {
        0              => "Clear sky",
        1              => "Mainly clear",
        2              => "Partly cloudy",
        3              => "Overcast",
        45 or 48       => "Foggy",
        51 or 53 or 55 => "Drizzle",
        61 or 63 or 65 => "Rain",
        71 or 73 or 75 => "Snowfall",
        77             => "Snow grains",
        80 or 81 or 82 => "Rain showers",
        85 or 86       => "Snow showers",
        95             => "Thunderstorm",
        96 or 99       => "Thunderstorm with hail",
        _              => $"Weather code {code}"
    };

    private static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // ── DTO Models ────────────────────────────────────────────────────────────

    private sealed record WeatherPayload(
        [property: JsonPropertyName("conversationId")] string ConversationId,
        [property: JsonPropertyName("city")] string City,
        [property: JsonPropertyName("country")] string? Country);

    private sealed record WeatherResult(
        [property: JsonPropertyName("city")] string City,
        [property: JsonPropertyName("country")] string? Country,
        [property: JsonPropertyName("temperatureCelsius")] double TemperatureCelsius,
        [property: JsonPropertyName("windSpeedKmh")] double WindSpeedKmh,
        [property: JsonPropertyName("weatherDescription")] string WeatherDescription,
        [property: JsonPropertyName("retrievedAt")] DateTime RetrievedAt);

    private sealed class GeocodingResponse
    {
        [JsonPropertyName("results")]
        public List<GeocodingResult>? Results { get; set; }
    }

    private sealed class GeocodingResult
    {
        [JsonPropertyName("latitude")]  public double Latitude  { get; set; }
        [JsonPropertyName("longitude")] public double Longitude { get; set; }
        [JsonPropertyName("country")]   public string? Country  { get; set; }
    }

    private sealed class OpenMeteoResponse
    {
        [JsonPropertyName("current")]
        public CurrentWeather? Current { get; set; }
    }

    private sealed class CurrentWeather
    {
        [JsonPropertyName("temperature_2m")]   public double Temperature2m  { get; set; }
        [JsonPropertyName("wind_speed_10m")]   public double WindSpeed10m   { get; set; }
        [JsonPropertyName("weather_code")]     public int    WeatherCode    { get; set; }
    }
}
