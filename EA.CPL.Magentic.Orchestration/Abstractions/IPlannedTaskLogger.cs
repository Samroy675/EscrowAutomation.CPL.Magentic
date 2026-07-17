using EA.CPL.Magentic.Orchestration.Models;

namespace EA.CPL.Magentic.Orchestration.Abstractions;

public interface IPlannedTaskLogger
{
    Task LogAsync(
        PlannedTask plannedTask,
        CancellationToken cancellationToken = default);
}
