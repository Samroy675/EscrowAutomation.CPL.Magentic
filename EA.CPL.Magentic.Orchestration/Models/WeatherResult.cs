using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace EA.CPL.Magentic.Orchestration.Models
{
    public sealed record WeatherResult(
    [property: JsonPropertyName("city")] string City,
    [property: JsonPropertyName("country")] string? Country,
    [property: JsonPropertyName("temperatureCelsius")] double TemperatureCelsius,
    [property: JsonPropertyName("windSpeedKmh")] double WindSpeedKmh,
    [property: JsonPropertyName("weatherDescription")] string WeatherDescription,
    [property: JsonPropertyName("retrievedAt")] DateTime RetrievedAt);
}
