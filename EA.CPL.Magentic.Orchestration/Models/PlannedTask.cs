namespace EA.CPL.Magentic.Orchestration.Models;

public class PlannedTask
{
    public string OrchestrationId { get; set; } = string.Empty;

    public string TaskName { get; set; } = string.Empty;

    public string TaskDescription { get; set; } = string.Empty;

    public string TaskStatus { get; set; } = string.Empty;

    public DateTime CreatedOnUtc { get; set; }

    public MagenticOrchestrationRequest? Request { get; set; }
}
