// Copyright (c) Microsoft. All rights reserved.

using EA.CPL.Magentic.Orchestration.Abstractions;
using EA.CPL.Magentic.Orchestration.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace EA.CPL.Magentic.Orchestration.Agents;

public class SPSOrderReadAgent(ILogger<SPSOrderReadAgent> logger, IChatClient client, IServiceBusPublisher publisher) : IAiAgent
{
    public string Name => "SPSOrderRead";

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
            Id = "sps-order-read-v1",
            Name = Name,
            Description = "Publishes SPS Order Read requests to Azure Service Bus for async processing by the SPS Order Function App.",
            ChatOptions = new ChatOptions
            {
                Instructions = $"""
                    You are the SPSOrderReadAgent. Your only responsibility is to publish SPS order read requests to Azure Service Bus.
                    When instructed to read an SPS order, call the publishSpsOrderReadRequestTool with:
                      - orderNumber: the order number to retrieve
                      - workflowConversationId: the conversation id provided to you
                      - sourceSystem: the source system (default: "RPA Bot - DSG")
                      - sourceAccount: the source account (default: "RPA-DSG-SPSAPI-LU-NP")
                      - profile: the profile (default: "Default\\p\\cac")
                    After calling the tool, report the outcome back to the orchestrator.
                    Orchestration Id: {context.Request.OrchestrationId}
                    Order Number: {context.Request.OrderNumber}
                    """,
                Tools = [GetPublishSpsOrderReadRequestTool(context)]
            }
        });

        return response;
    }

    private AITool GetPublishSpsOrderReadRequestTool(AgentExecutionContext context)
    {
        // Capture all values from context so the LLM only needs to supply the conversationId
        string orderNumber   = context.Request.OrderNumber   ?? string.Empty;
        string sourceSystem  = context.Request.SourceSystem  ?? string.Empty;
        string sourceAccount = context.Request.SourceAccount ?? string.Empty;
        string profile       = context.Request.Profile       ?? string.Empty;

        AIFunction tool = AIFunctionFactory.Create(
        async ([Description("The workflow conversation id")] string workflowConversationId) =>
        {
            var message = new JobMessage
            {
                TargetSubscriber = Subscribers.SPSOrderRead,
                ConversationId   = workflowConversationId,
                TaskId           = Guid.NewGuid().ToString(),
                AgentName        = Name,
                TaskDescription  = $"Read SPS order {orderNumber}",
                OrderNumber      = orderNumber,
                SourceSystem     = sourceSystem,
                SourceAccount    = sourceAccount,
                Profile          = profile
            };

            await publisher.SendMessage(message);
            Console.WriteLine($"[SPSOrderReadAgent] Published SPS order read request for order '{orderNumber}' (conversationId={workflowConversationId})");
            return $"SPS order read request for order '{orderNumber}' published to Service Bus. " +
                   $"The SPS Order Function App will process it and store the result keyed to conversationId '{workflowConversationId}'.";
        },
        "publishSpsOrderReadRequestTool",
        "Publishes an SPS Order Read request to Azure Service Bus for async processing.");

        return tool;
    }
}

