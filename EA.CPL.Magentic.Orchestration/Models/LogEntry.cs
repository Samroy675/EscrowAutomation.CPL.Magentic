namespace EA.CPL.Magentic.Orchestration.Models;

public record LogEntry
(
    string OrchestrationId,
    DateTime TimestampUtc,
    string Source,
    string Message
);
