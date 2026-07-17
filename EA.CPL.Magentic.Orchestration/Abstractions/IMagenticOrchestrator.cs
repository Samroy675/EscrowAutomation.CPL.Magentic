using EA.CPL.Magentic.Orchestration.Models;

namespace EA.CPL.Magentic.Orchestration.Abstractions;

public interface IMagenticOrchestrator
{
    Task<MagenticOrchestrationResult> RunAsync(
        MagenticOrchestrationRequest request,
        CancellationToken cancellationToken = default);
}
