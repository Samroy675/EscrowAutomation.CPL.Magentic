namespace EA.CPL.Magentic.Orchestration.Models;

public class AgentExecutionContext
{
    public MagenticOrchestrationRequest Request { get; set; } = new();

    public Dictionary<string, object> Data { get; set; } = [];
}
