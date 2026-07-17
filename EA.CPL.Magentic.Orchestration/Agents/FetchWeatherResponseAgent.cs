using EA.CPL.Magentic.Orchestration.Abstractions;
using EA.CPL.Magentic.Orchestration.Models;
using EA.CPL.Magentic.Orchestration.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace EA.CPL.Magentic.Orchestration.Agents
{
    public class FetchWeatherResponseAgent(ILogger<FetchWeatherResponseAgent> logger, IChatClient client, WeatherResultStore resultStore) : IAiAgent
    {
        public string Name => "FetchWeatherResponse";

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
                Id = "fetch-weather-response--v1",
                Name = "FetchWeatherResponse",
                Description = "Fetches the weather result from Azure Blob Storage and presents it on the console.",
                ChatOptions = new ChatOptions
                {
                    Instructions = """
                        You are the FetchResponseAgent. Your job is to retrieve the weather result and display it clearly.
                        Call FetchWeatherResult with the conversation id, then present the weather data in a friendly summary.
                        If the result is not yet available, report that back so the orchestrator knows to resume later.
                        """,
                    Tools = [GetFetchWeatherResultTool()]
                }
            });

            return response;
        }

        private AITool GetFetchWeatherResultTool()
        {
            AIFunction fetchWeatherResultTool = AIFunctionFactory.Create(
           async ([Description("The workflow conversation id")] string workflowConversationId) =>
           {
               Console.WriteLine($"[FetchResponseAgent] Polling for weather result (conversationId={workflowConversationId})...");
               WeatherResult? result = await resultStore.PollForResultAsync(workflowConversationId, timeoutSeconds: 90);

               if (result is null)
               {
                   return $"Weather result not yet available. Resume with: dotnet run -- --conversationid={workflowConversationId}";
               }

               return $"""
                    Weather result for {result.City}{(result.Country is not null ? $", {result.Country}" : "")}:
                    - Temperature  : {result.TemperatureCelsius:F1} degrees C
                    - Wind Speed   : {result.WindSpeedKmh:F1} km/h
                    - Condition    : {result.WeatherDescription}
                    - Retrieved At : {result.RetrievedAt:u} UTC
                    """;
           },
           "FetchWeatherResult",
           "Polls Azure Blob Storage for the weather result stored by the Azure Function for the given conversation id.");
            return fetchWeatherResultTool;
        }
    }
}
