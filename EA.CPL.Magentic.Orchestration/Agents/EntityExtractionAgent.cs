using EA.CPL.Magentic.Orchestration.Abstractions;
using EA.CPL.Magentic.Orchestration.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace EA.CPL.Magentic.Orchestration.Agents;

public class EntityExtractionAgent(ILogger<EntityExtractionAgent> logger,IChatClient client) : IAiAgent
{
    public string Name => "EntityExtraction";

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
            Id = "entity-extraction-v1",
            Name = Name,
            Description = "Entity Extraction Agent",
            ChatOptions = new ChatOptions
            {
                Instructions = $"""
                        TODO - Add instructions for the Entity Extraction Agent here.
                        """
            }
        });

        return response;
    }
    
}
