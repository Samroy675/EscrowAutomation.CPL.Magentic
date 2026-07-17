using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SPSOrderFunctionApp.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SPSOrderFunctionApp;

/// <summary>
/// Azure Function triggered by messages on the 'sps-order-requests' Service Bus topic.
/// Calls the SPS Order Read API and stores the result in Azure Blob Storage
/// keyed by conversationId so the orchestrator agent can retrieve it.
/// </summary>
public class SpsOrderTriggerFunction
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SpsOrderTriggerFunction> _logger;

    private const string ContainerName = "sps-order-results";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SpsOrderTriggerFunction(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<SpsOrderTriggerFunction> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    [Function("SpsOrderTrigger")]
    public async Task Run(
        [ServiceBusTrigger(
            topicName: "%SPS_SERVICE_BUS_TOPIC_NAME%",
            subscriptionName: "%SPS_SERVICE_BUS_SUBSCRIPTION_NAME%",
            Connection = "SERVICE_BUS_CONNECTION_STRING")]
        string messageBody,
        FunctionContext context)
    {
        _logger.LogInformation("SpsOrderTrigger received message: {Body}", messageBody);

        SpsOrderRequestMessage? message = JsonSerializer.Deserialize<SpsOrderRequestMessage>(messageBody, JsonOptions);
        if (message is null)
        {
            _logger.LogError("Failed to deserialize SPS order request message.");
            return;
        }

        _logger.LogInformation(
            "Processing SPS order read for OrderNumber='{OrderNumber}', ConversationId='{ConvId}'",
            message.OrderNumber, message.ConversationId);

        try
        {
            (string status, string rawJson, string? errorDetail) = await CallSpsOrderReadApiAsync(message);

            if (status != "Success")
            {
                _logger.LogError("SPS API returned non-success status for order '{OrderNumber}': {Error}",
                    message.OrderNumber, errorDetail);
                await StoreRawAsync(message.ConversationId,
                    JsonSerializer.Serialize(new { Error = errorDetail, OrderNumber = message.OrderNumber }));
                return;
            }

            _logger.LogInformation("SPS API call succeeded for order '{OrderNumber}'.", message.OrderNumber);
            await StoreRawAsync(message.ConversationId, rawJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing SPS order '{OrderNumber}'.", message.OrderNumber);
            await StoreRawAsync(message.ConversationId,
                JsonSerializer.Serialize(new { Error = ex.Message, OrderNumber = message.OrderNumber }));
        }
    }

    private async Task<(string Status, string RawJson, string? ErrorDetail)> CallSpsOrderReadApiAsync(SpsOrderRequestMessage message)
    {
        string apiUrl = _configuration["SPS_API_URL"]
            ?? "https://sps-wp-uat.fnf.com/api/v1/Order/Read";
        string basicAuthToken = _configuration["SPS_BASIC_AUTH_TOKEN"] ?? string.Empty;

        var requestBody = new SpsApiRequest
        {
            DocumentVersion = "2.0",
            Service = new SpsService
            {
                Request = new SpsServiceRequest
                {
                    SourceSystem = message.SourceSystem,
                    SourceAccount = message.SourceAccount
                },
                Select = new SpsServiceSelect
                {
                    Profile = message.Profile
                }
            },
            Payload = new SpsPayload
            {
                Select = new SpsPayloadSelect
                {
                    Order = new SpsOrderSelect
                    {
                        OrderNumber = message.OrderNumber,
                        UseDefaults = false,
                        OrderData = new SpsOrderDataSelect()
                    }
                }
            }
        };

        string json = JsonSerializer.Serialize(requestBody);
        _logger.LogInformation("SPS API request body: {RequestBody}", json);

        HttpClient httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrWhiteSpace(basicAuthToken))
        {
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", basicAuthToken);
        }

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await httpClient.PostAsync(apiUrl, content);

        string responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("SPS API returned {StatusCode}: {ErrorBody}", (int)response.StatusCode, responseBody);
            response.EnsureSuccessStatusCode();
        }

        _logger.LogInformation("SPS API raw response: {Response}", responseBody);

        // Parse only Status and Messages using JsonDocument to avoid fragile model mapping
        using JsonDocument doc = JsonDocument.Parse(responseBody);
        string status = doc.RootElement
            .GetProperty("Result")
            .GetProperty("Status")
            .GetString() ?? string.Empty;

        string? errorDetail = null;
        if (status != "Success")
        {
            var messages = doc.RootElement.GetProperty("Result").GetProperty("Messages");
            if (messages.ValueKind == JsonValueKind.Array && messages.GetArrayLength() > 0)
                errorDetail = string.Join("; ", messages.EnumerateArray().Select(m => m.GetString()));
            else
                errorDetail = "Unknown error";
        }

        return (status, responseBody, errorDetail);
    }

    private async Task StoreRawAsync(string conversationId, string json)
    {
        string storageConnection = _configuration["AZURE_STORAGE_CONNECTION_STRING"]
            ?? throw new InvalidOperationException("AZURE_STORAGE_CONNECTION_STRING is not configured.");

        BlobServiceClient blobServiceClient = new(storageConnection);
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
        await containerClient.CreateIfNotExistsAsync();

        string blobName = $"{conversationId}.json";
        BlobClient blobClient = containerClient.GetBlobClient(blobName);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        await blobClient.UploadAsync(stream, overwrite: true);

        _logger.LogInformation("Stored SPS order result in blob '{BlobName}'.", blobName);
    }
}
