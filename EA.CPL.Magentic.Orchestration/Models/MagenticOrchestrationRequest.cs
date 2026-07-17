namespace EA.CPL.Magentic.Orchestration.Models;

public class MagenticOrchestrationRequest
{
    /// <summary>
    /// User feedback on the plan. Starts with "plan approved" (case-insensitive) to approve,
    /// or provide revision instructions e.g. "revise plan to add X".
    /// </summary>
    public string? PlanFeedback { get; set; }
    
    public bool IsPlanApproved { get; set; }
    public string? OrchestrationId { get; set; } = null;

    public string? State { get; set; }

    public string? County { get; set; }

    public string? Profile { get; set; }

    public string? SourceSystem { get; set; }

    public string? SourceAccount { get; set; }

    public string? OrderNumber { get; set; }

    public string? OrderType { get; set; }

    public string? ProfileType { get; set; }
}
