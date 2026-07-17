using EA.CPL.Magentic.Orchestration.Abstractions;
using EA.CPL.Magentic.Orchestration.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace EA.CPL.Magentic.Orchestration.Agents
{
    public class InvokeWeatherAgent(ILogger<InvokeWeatherAgent> logger, IChatClient client, IServiceBusPublisher publisher) : IAiAgent
    {
        public string Name => "InvokeWeather";

        public AIAgent CreateAsync(AgentExecutionContext context, CancellationToken cancellationToken = default)
        {
            logger.LogInformation(
                "Creating placeholder {AgentName} agent for profile {Profile}",
                Name,
                context.Request.Profile);

            var response = new ChatClientAgent(
            client,
            new ChatClientAgentOptions
            {
                Id = "invoke-weather-v1",
                Name = "InvokeWeather",
                Description = "Publishes weather API requests to Azure Service Bus for async processing.",
                ChatOptions = new ChatOptions
                {
                    Instructions = """
                        You are the CallHistoricalWeatherApiAgent. Your only responsibility is to publish historical weather requests to Azure Service Bus.
                        When instructed to get weather for a city, call publishHistoricalWeatherRequestTool with:
                          - city: the city name
                          - workflowConversationId: the conversation id provided to you
                          - country: the ISO country code if known
                        After calling the tool, report the outcome back to the orchestrator.
                        """,
                    Tools = [GetPublishWeatherRequestTool()]
                }
            });

            return response;
        }

        private AITool GetPublishWeatherRequestTool()
        {
            AIFunction publishWeatherRequestTool = AIFunctionFactory.Create(
            async (
                [Description("The city name to get weather for, e.g. London")] string city,
                [Description("The workflow conversation id")] string workflowConversationId,
                [Description("Optional ISO country code, e.g. GB")] string? country = null) =>
            {
               JobMessage message = new JobMessage
                {
                    City= city,
                    Country = country,
                    TargetSubscriber = "WeatherFunction",
                    ConversationId = workflowConversationId,
                    TaskId = Guid.NewGuid().ToString(),
                    AgentName = "InvokeWeatherAgent",
                    TaskDescription = $"Request weather for {city}, {country}",
                    MessageCreated = DateTimeOffset.UtcNow
                };

                await publisher.SendMessage(message);
                Console.WriteLine($"[InvokeWeatherAgent] Published weather request for '{city}' (conversationId={workflowConversationId})");
                return $"Weather request for '{city}' published  " +
                       $"The Azure Function will process it and store the result keyed to conversationId '{workflowConversationId}'.";
            },
            "PublishWeatherRequest",
            "Publishes a weather data request to the Azure Service Bus topic for async processing by an Azure Function.");
            return publishWeatherRequestTool;
        }
            
    }
}
