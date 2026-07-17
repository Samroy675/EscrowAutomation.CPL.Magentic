using EA.CPL.Magentic.Orchestration.Models;
using Microsoft.Agents.AI;

namespace EA.CPL.Magentic.Orchestration.Abstractions;

public interface IAiAgent
{
    string Name { get; }

    AIAgent CreateAsync(AgentExecutionContext context,
        CancellationToken cancellationToken = default);
}
