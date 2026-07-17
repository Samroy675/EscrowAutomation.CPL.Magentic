using EA.CPL.Magentic.Orchestration.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace EA.CPL.Magentic.Orchestration.Models
{
    public sealed class ConversationSession
    {
        [JsonPropertyName("conversationid")] public string ConversationId { get; set; } = string.Empty;
        [JsonPropertyName("workflowstage")] public WorkflowStage WorkflowStage { get; set; } = WorkflowStage.NotStarted;
        [JsonPropertyName("latestplantxt")] public string? LatestPlanText { get; set; }
        [JsonPropertyName("messages")] public List<ConversationRecord> Messages { get; set; } = [];
    }

    public sealed record ConversationRecord(string Role, string? AuthorName, string Text);

}
