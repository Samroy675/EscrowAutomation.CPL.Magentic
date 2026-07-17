using Azure.Messaging.ServiceBus;
using EA.CPL.Magentic.Orchestration.Abstractions;
using EA.CPL.Magentic.Orchestration.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EA.CPL.Magentic.Orchestration.Services
{
    public class ServiceBusPublisher : IServiceBusPublisher
    {
        private readonly ServiceBusClient _client;
        private readonly ServiceBusSender _sender;
        private readonly ILogger<ServiceBusPublisher> _logger;

        public ServiceBusPublisher(string connectionString, string topicName, ILogger<ServiceBusPublisher> logger)
        {
            _logger = logger;
            _client = new ServiceBusClient(connectionString);
            _sender = _client.CreateSender(topicName);
        }
        public async Task SendMessage(JobMessage message, CancellationToken ct = default)
        {
            var body = JsonSerializer.Serialize(message);

            var serviceBusMessage = new ServiceBusMessage(body)
            {
                ApplicationProperties = { { "subscriber", $"{message.TargetSubscriber}" } },
                ContentType = "application/json",
                CorrelationId = message.ConversationId,
                MessageId = $"{message.ConversationId}-magentic-extraction",
                Subject = "Magentic-CPL-Extraction"
            };

            await _sender.SendMessageAsync(serviceBusMessage, ct);
            _logger.LogInformation("Message sent to Service Bus.\n{Body}", body);
        }
    }
}