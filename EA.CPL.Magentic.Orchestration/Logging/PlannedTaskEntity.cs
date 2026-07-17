using Azure;
using Azure.Data.Tables;

namespace EA.CPL.Magentic.Orchestration.Logging;

public class PlannedTaskEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;

    public string RowKey { get; set; } = string.Empty;

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }

    public string OrchestrationId { get; set; } = string.Empty;

    public string TaskName { get; set; } = string.Empty;

    public string TaskDescription { get; set; } = string.Empty;

    public string TaskStatus { get; set; } = string.Empty;

    public DateTime CreatedOnUtc { get; set; }

    public string? State { get; set; }

    public string? County { get; set; }

    public string? Profile { get; set; }

    public string? SourceSystem { get; set; }

    public string? SourceAccount { get; set; }

    public string? OrderNumber { get; set; }

    public string? OrderType { get; set; }

    public string? ProfileType { get; set; }
}
