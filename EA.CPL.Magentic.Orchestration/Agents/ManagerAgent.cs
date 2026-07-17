using Azure;
using EA.CPL.Magentic.Orchestration.Abstractions;
using EA.CPL.Magentic.Orchestration.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace EA.CPL.Magentic.Orchestration.Agents
{
    public class ManagerAgent(ILogger<ManagerAgent> logger,IChatClient client) : IAiAgent
    {
        public string Name => "Manager";

        public AIAgent CreateAsync(AgentExecutionContext context,CancellationToken cancellationToken = default)
        {
            logger.LogInformation(
                "Creating placeholder {AgentName} agent for profile {Profile}",
                Name,
                context.Request.Profile);

            var response =  new ChatClientAgent(
            client,
            new ChatClientAgentOptions
            {
                Id = "magentic-manager-v1",
                Name = "MagenticManager",
                Description = "Orchestrates the weather retrieval workflow.",
                ChatOptions = new ChatOptions
                {
                    Instructions = $"""
                        You coordinate the weather retrieval workflow. Orchestration Id: {context.Request.OrchestrationId}
                        Create a plan to achieve the task using the provided agents.
                        Ensure that the plan is clear and sequential, and request approval before executing it.

                        When you receive feedback on a plan:
                        - If the feedback expresses approval (e.g. "looks good", "approved", "go ahead", "yes", "proceed"), treat the plan as approved and begin execution.
                        - If the feedback requests changes (e.g. "change X", "add Y", "revise to..."), update the plan accordingly and request approval again.
                        """
                }
            });

            return response;
        }

    }
}
