using EA.CPL.Magentic.Orchestration.Abstractions;
using EA.CPL.Magentic.Orchestration.Models;
using EA.CPL.Magentic.Orchestration.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace EA.CPL.Magentic.Orchestration.Agents;

public class FetchSpsOrderResponseAgent(
    ILogger<FetchSpsOrderResponseAgent> logger,
    IChatClient client,
    SpsOrderResultStore resultStore) : IAiAgent
{
    public string Name => "FetchSpsOrderResponse";

    public AIAgent CreateAsync(AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Creating {AgentName} agent for order {OrderNumber}",
            Name,
            context.Request.OrderNumber);

        var response = new ChatClientAgent(
        client,
        new ChatClientAgentOptions
        {
            Id = "fetch-sps-order-response-v1",
            Name = Name,
            Description = "Fetches the SPS Order Read result from Azure Blob Storage and presents it as the final output.",
            ChatOptions = new ChatOptions
            {
                Instructions = $"""
                    You are the FetchSpsOrderResponseAgent. Your job is to retrieve the SPS order result from blob storage and present it as the final workflow output.
                    Call fetchSpsOrderResultTool with the conversation id to retrieve the result.
                    Once retrieved, present the full order data clearly and concisely as the final response to the user.
                    If the result is not yet available, report that back so the orchestrator knows to wait and retry.
                    Orchestration Id: {context.Request.OrchestrationId}
                    """,
                Tools = [GetFetchSpsOrderResultTool()]
            }
        });

        return response;
    }

    private AITool GetFetchSpsOrderResultTool()
    {
        AIFunction fetchTool = AIFunctionFactory.Create(
        async ([Description("The workflow conversation id")] string workflowConversationId) =>
        {
            Console.WriteLine($"[FetchSpsOrderResponseAgent] Polling for SPS order result (conversationId={workflowConversationId})...");

            string? json = await resultStore.PollForResultAsync(workflowConversationId, timeoutSeconds: 120);

            if (json is null)
            {
                return $"SPS order result not yet available for conversationId '{workflowConversationId}'. The function app may still be processing. Please retry shortly.";
            }

            Console.WriteLine($"[FetchSpsOrderResponseAgent] SPS order result retrieved for conversationId={workflowConversationId}");
            return $"SPS Order Read Result:\n{json}";
        },
        "fetchSpsOrderResultTool",
        "Polls Azure Blob Storage for the SPS order result stored by the SPS Order Function App for the given conversation id.");

        return fetchTool;
    }
}
