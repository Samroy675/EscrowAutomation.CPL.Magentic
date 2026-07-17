using EA.CPL.Magentic.Orchestration.Enums;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace EA.CPL.Magentic.Orchestration.Models
{
    public sealed class ConversationSessionState
    {
        public WorkflowStage WorkflowStage { get; set; } = WorkflowStage.NotStarted;
        public string? LatestPlanText { get; set; }
        public List<ChatMessage> Messages { get; set; } = [];
    }
}
