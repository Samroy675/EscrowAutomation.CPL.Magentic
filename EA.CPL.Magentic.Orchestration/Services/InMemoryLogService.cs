using EA.CPL.Magentic.Orchestration.Abstractions;
using EA.CPL.Magentic.Orchestration.Models;

namespace EA.CPL.Magentic.Orchestration.Services;

public class InMemoryLogService : ILogService
{
    private readonly List<LogEntry> _logs = new();

    public event Action<LogEntry>? LogAppended;

    public Task AppendLogAsync(LogEntry entry, CancellationToken cancellationToken = default)
    {
        lock (_logs)
        {
            _logs.Add(entry);
        }

        LogAppended?.Invoke(entry);
        return Task.CompletedTask;
    }

    public IReadOnlyList<LogEntry> GetLogs(string orchestrationId)
    {
        lock (_logs)
        {
            return _logs.Where(l => l.OrchestrationId == orchestrationId).ToList();
        }
    }
}
