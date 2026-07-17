namespace EA.CPL.Magentic.Orchestration.Models;

public class MagenticOrchestrationResult
{
    public string? OrchestrationId { get; set; }

    public string Status { get; set; } = "Completed";

    public string? FinalResponse { get; set; }

    public List<AgentResponse> AgentResponses { get; set; } = [];

    public DateTime StartedOnUtc { get; set; }

    public DateTime CompletedOnUtc { get; set; }
}
