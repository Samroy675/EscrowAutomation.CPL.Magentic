using Azure.Storage.Blobs;
using EA.CPL.Magentic.Orchestration.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace EA.CPL.Magentic.Orchestration.Services
{
    public sealed class WeatherResultStore
    {
        private readonly BlobContainerClient _container;
        private const string ContainerName = "weather-results";

        public WeatherResultStore(string connectionString)
        {
            _container = new BlobContainerClient(connectionString, ContainerName);
        }

        /// <summary>
        /// Attempts to retrieve the weather result for a given conversation id.
        /// Returns null if the result is not yet available.
        /// </summary>
        public async Task<WeatherResult?> TryGetResultAsync(string conversationId, CancellationToken cancellationToken = default)
        {
            try
            {
                BlobClient blob = _container.GetBlobClient($"{conversationId}.json");

                if (!await blob.ExistsAsync(cancellationToken))
                {
                    return null;
                }

                var response = await blob.DownloadContentAsync(cancellationToken);
                string json = response.Value.Content.ToString();

                return JsonSerializer.Deserialize<WeatherResult>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Polls for the weather result until it becomes available or the timeout elapses.
        /// Returns null if the result does not arrive within the timeout.
        /// </summary>
        public async Task<WeatherResult?> PollForResultAsync(
            string conversationId,
            int timeoutSeconds = 90,
            int pollIntervalSeconds = 3,
            CancellationToken cancellationToken = default)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

            while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
            {
                WeatherResult? result = await TryGetResultAsync(conversationId, cancellationToken);
                if (result is not null)
                {
                    return result;
                }

                await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds), cancellationToken);
            }

            return null;
        }
    }
}
