using EA.CPL.Magentic.Orchestration.Models;

namespace EA.CPL.Magentic.Orchestration.Abstractions;

public interface ILogService
{
    event Action<LogEntry>? LogAppended;

    Task AppendLogAsync(LogEntry entry, CancellationToken cancellationToken = default);

    IReadOnlyList<LogEntry> GetLogs(string orchestrationId);
}
