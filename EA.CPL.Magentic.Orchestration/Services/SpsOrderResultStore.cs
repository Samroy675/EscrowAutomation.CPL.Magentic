using Azure.Storage.Blobs;
using System.Text.Json;

namespace EA.CPL.Magentic.Orchestration.Services;

public sealed class SpsOrderResultStore
{
    private readonly BlobContainerClient _container;
    private const string ContainerName = "sps-order-results";

    public SpsOrderResultStore(string connectionString)
    {
        _container = new BlobContainerClient(connectionString, ContainerName);
    }

    /// <summary>
    /// Attempts to retrieve the SPS order result for a given conversation id.
    /// Returns null if the result is not yet available.
    /// </summary>
    public async Task<string?> TryGetResultAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            BlobClient blob = _container.GetBlobClient($"{conversationId}.json");

            if (!await blob.ExistsAsync(cancellationToken))
                return null;

            var response = await blob.DownloadContentAsync(cancellationToken);
            return response.Value.Content.ToString();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Polls for the SPS order result until it becomes available or the timeout elapses.
    /// </summary>
    public async Task<string?> PollForResultAsync(
        string conversationId,
        int timeoutSeconds = 120,
        int pollIntervalSeconds = 3,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            string? result = await TryGetResultAsync(conversationId, cancellationToken);
            if (result is not null)
                return result;

            await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds), cancellationToken);
        }
        return null;
    }
}
