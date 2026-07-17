using System;
using System.Collections.Generic;
using System.Text;

namespace EA.CPL.Magentic.Orchestration.Enums
{
    public enum WorkflowStage
    {
        NotStarted,
        AwaitingPlanApproval,
        PlanApproved,
        Completed,
    }
}
