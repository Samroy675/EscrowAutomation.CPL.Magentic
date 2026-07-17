namespace EA.CPL.Magentic.Orchestration.Models;

public class AgentResponse
{
    public string AgentName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? Message { get; set; }

    public object? Data { get; set; }

    public DateTime CompletedOnUtc { get; set; }
}
