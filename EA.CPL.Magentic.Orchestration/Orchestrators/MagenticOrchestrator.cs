using EA.CPL.Magentic.Orchestration.Abstractions;
using EA.CPL.Magentic.Orchestration.Agents;
using EA.CPL.Magentic.Orchestration.Enums;
using EA.CPL.Magentic.Orchestration.Models;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Checkpointing;
using Microsoft.Agents.AI.Workflows.InProc;
using Microsoft.Agents.AI.Workflows.Specialized.Magentic;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.Realtime;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EA.CPL.Magentic.Orchestration.Orchestrators;

public class MagenticOrchestrator(
    //IPlannedTaskLogger plannedTaskLogger,
    ManagerAgent managerAgentHelper,
    InvokeWeatherAgent invokeWeatherAgentHelper,
    FetchWeatherResponseAgent fetchWeatherResponseAgentHelper,
    SPSOrderReadAgent spsOrderReadAgentHelper,
    FetchSpsOrderResponseAgent fetchSpsOrderResponseAgentHelper,
    ILogger<MagenticOrchestrator> logger,
    IChatClient chatClient,
    IConversationService conversationService,
    EA.CPL.Magentic.Orchestration.Abstractions.ILogService logService) : IMagenticOrchestrator
{
    public async Task<MagenticOrchestrationResult> RunAsync(
        MagenticOrchestrationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.OrchestrationId = string.IsNullOrWhiteSpace(request.OrchestrationId)
            ? Guid.NewGuid().ToString("N")
            : request.OrchestrationId;

        var startedOnUtc = DateTime.UtcNow;
        logger.LogInformation(
            "Starting orchestration {OrchestrationId} for order {OrderNumber}",
            request.OrchestrationId,
            request.OrderNumber);

        // Emit startup log
        try
        {
            await logService.AppendLogAsync(new Models.LogEntry(request.OrchestrationId ?? string.Empty, DateTime.UtcNow, "Orchestrator", "Starting orchestration"), cancellationToken);
        }
        catch { /* best-effort logging */ }

        string taskPrompt = $"Get the current weather for London, UK. Conversation id: {request.OrchestrationId}";

       // string taskPrompt = $"""Read SPS order {request.OrderNumber} using the SPSOrderRead agent. Conversation id: {request.OrchestrationId}. """;
        var executionContext = new AgentExecutionContext
        {
            Request = request,
            Data = new Dictionary<string, object>()
        };

        Workflow workflow = new MagenticWorkflowBuilder(managerAgentHelper.CreateAsync(executionContext))
            .AddParticipants([invokeWeatherAgentHelper.CreateAsync(executionContext), fetchWeatherResponseAgentHelper.CreateAsync(executionContext), spsOrderReadAgentHelper.CreateAsync(executionContext), fetchSpsOrderResponseAgentHelper.CreateAsync(executionContext)])
            .WithName("Weather API Magentic Workflow")
            .WithDescription("Retrieve live weather data via Service Bus and Azure Functions using Open-Meteo API")
            //.WithDescription("Retrieve SPS Order data via Service Bus and Azure Functions using SPS Order Read API")
            .RequirePlanSignoff(true)
            .WithMaxRounds(10)
            .WithMaxStalls(3)
            .WithMaxResets(2)
            .Build();

        DirectoryInfo checkpointDirectory = new(Path.Combine(AppContext.BaseDirectory, "checkpoints"));
        Console.WriteLine($"Checkpoint store: {checkpointDirectory.FullName}");

        using FileSystemJsonCheckpointStore checkpointStore = new(checkpointDirectory);
        CheckpointManager checkpointManager = CheckpointManager.CreateJson(checkpointStore);
        InProcessExecutionEnvironment environment = InProcessExecution.Lockstep.WithCheckpointing(checkpointManager);

        ConversationSessionState sessionState = conversationService.LoadConversationSession(request.OrchestrationId);
        List<ChatMessage> conversationHistory = sessionState.Messages;

        Console.WriteLine("Building Magentic workflow...");
        Console.WriteLine();
        Console.WriteLine($"Task: {taskPrompt}");
        Console.WriteLine();
        Console.WriteLine("Starting workflow execution...");
        Console.WriteLine();

        CheckpointInfo? latestCheckpoint = await conversationService.GetLatestCheckpointAsync(checkpointStore, request.OrchestrationId);
        StreamingRun? run = null;
        bool resumedFromCheckpoint = false;

        if (latestCheckpoint is not null)
        {
            Console.WriteLine($"Found checkpoint {latestCheckpoint.CheckpointId} - attempting resume...");
            try
            {
                run = await environment.ResumeStreamingAsync(workflow, latestCheckpoint);
                resumedFromCheckpoint = true;
                Console.WriteLine("Resumed from engine checkpoint successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Checkpoint incompatible ({ex.GetType().Name}): {ex.Message}");
                Console.WriteLine("Purging stale checkpoints - starting fresh.");
                //await PurgeCheckpointsForConversationAsync(checkpointStore, checkpointDirectory);
                run = null;
            }
        }

        if (run is null)
        {
            List<ChatMessage> seedMessages = [new ChatMessage(ChatRole.User, taskPrompt)];
            Console.WriteLine("Starting fresh run.");
            run = await environment.RunStreamingAsync(workflow, seedMessages, request.OrchestrationId);
            await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
        }

        Console.WriteLine("Waiting for the first workflow event...");
        Console.WriteLine();

        bool autoApproveNextPlan = !resumedFromCheckpoint && sessionState.WorkflowStage is WorkflowStage.PlanApproved;
        var result = new MagenticOrchestrationResult
        {
            OrchestrationId = request.OrchestrationId,
            StartedOnUtc = startedOnUtc,
            CompletedOnUtc = DateTime.UtcNow
        };
        WorkflowOutputEvent? finalOutput = null;
        await using (run)
        {
            string? lastResponseId = null;
            string? latestPlanText = null;
            bool planNeedsApproval = sessionState.WorkflowStage is WorkflowStage.AwaitingPlanApproval;
            MagenticProgressLedger? latestProgressLedger = null;
            string? currentAgentId = null;
            var currentAgentTextBuilder = new System.Text.StringBuilder();

            await foreach (WorkflowEvent workflowEvent in run.WatchStreamAsync())
            {
                switch (workflowEvent)
                {
                    case AgentResponseUpdateEvent updateEvent:
                        if (latestProgressLedger is not null)
                        {
                            var ledgerText = FormatProgressLedger(latestProgressLedger);
                            WriteMagenticMessage("Progress Ledger", ledgerText);
                            AppendWorkflowHistory(conversationHistory, "Progress Ledger", ledgerText);
                            try { await logService.AppendLogAsync(new Models.LogEntry(request.OrchestrationId ?? string.Empty, DateTime.UtcNow, "ProgressLedger", ledgerText), cancellationToken); } catch {}
                            latestProgressLedger = null;
                        }

                        // Flush previous agent's accumulated response when a new agent starts
                        if (currentAgentId is not null && currentAgentId != updateEvent.ExecutorId)
                        {
                            result.AgentResponses.Add(new AgentResponse
                            {
                                AgentName = currentAgentId,
                                Message = currentAgentTextBuilder.ToString(),
                                Status = "Completed",
                                CompletedOnUtc = DateTime.UtcNow
                            });
                            currentAgentTextBuilder.Clear();
                        }

                        currentAgentId = updateEvent.ExecutorId;
                        currentAgentTextBuilder.Append(updateEvent.Update.Text);
                        AppendWorkflowHistory(conversationHistory, updateEvent.ExecutorId, updateEvent.Update.Text);
                        try { await logService.AppendLogAsync(new Models.LogEntry(request.OrchestrationId ?? string.Empty, DateTime.UtcNow, updateEvent.ExecutorId, updateEvent.Update.Text ?? string.Empty), cancellationToken); } catch {}
                        break;

                    case MagenticPlanCreatedEvent planCreated:
                        WriteMagenticMessage("Initial Plan", planCreated.FullTaskLedger.Text);
                        AppendWorkflowHistory(conversationHistory, "Magentic Plan", planCreated.FullTaskLedger.Text);
                        latestPlanText = planCreated.FullTaskLedger.Text;
                        planNeedsApproval = true;
                        sessionState.WorkflowStage = WorkflowStage.AwaitingPlanApproval;
                        sessionState.LatestPlanText = latestPlanText;
                        result.Status = "PlanCreated";
                        result.FinalResponse = planCreated.FullTaskLedger.Text;

                        try { await logService.AppendLogAsync(new Models.LogEntry(request.OrchestrationId ?? string.Empty, DateTime.UtcNow, "Plan", planCreated.FullTaskLedger.Text), cancellationToken); } catch {}

                        logger.LogInformation(
                            "Plan generated for orchestration {OrchestrationId}: Plan  - {PlanText}",
                            result.OrchestrationId,
                            planCreated.FullTaskLedger.Text);
                        return result;
                        //break;
                    case MagenticReplannedEvent replanned:
                        WriteMagenticMessage("Replanned", replanned.FullTaskLedger.Text);
                        AppendWorkflowHistory(conversationHistory, "Magentic Replan", replanned.FullTaskLedger.Text);
                        latestPlanText = replanned.FullTaskLedger.Text;
                        autoApproveNextPlan = false;
                        planNeedsApproval = true;
                        sessionState.WorkflowStage = WorkflowStage.AwaitingPlanApproval;
                        sessionState.LatestPlanText = latestPlanText;
                        result.Status = "Replanned";
                        result.FinalResponse = latestPlanText;

                        try { await logService.AppendLogAsync(new Models.LogEntry(request.OrchestrationId ?? string.Empty, DateTime.UtcNow, "Replan", latestPlanText), cancellationToken); } catch {}

                        logger.LogInformation(
                            "Plan regenerated for orchestration {OrchestrationId}: Plan  - {PlanText}",
                            result.OrchestrationId,
                            latestPlanText);
                        return result;

                    case RequestInfoEvent requestInfoEvent:
                        {
                            MagenticPlanReviewRequest? planReviewRequest = requestInfoEvent.Request.Data.As<MagenticPlanReviewRequest>();
                            if (planReviewRequest is null) break;

                            WriteMagenticMessage("Plan Review Request", planReviewRequest.Plan.Text);
                            AppendWorkflowHistory(conversationHistory, "Plan Review Request", planReviewRequest.Plan.Text);
                            try { await logService.AppendLogAsync(new Models.LogEntry(request.OrchestrationId ?? string.Empty, DateTime.UtcNow, "PlanReviewRequest", planReviewRequest.Plan.Text), cancellationToken); } catch {}

                            MagenticPlanReviewResponse planReviewResponse;
                            if (autoApproveNextPlan)
                            {
                                Console.WriteLine("[Resume] Plan was already approved in prior session - auto-approving.");
                                planReviewResponse = planReviewRequest.Approve();
                                sessionState.WorkflowStage = WorkflowStage.PlanApproved;
                                autoApproveNextPlan = false;
                            }
                            else
                            {
                                string feedback = !string.IsNullOrWhiteSpace(request.PlanFeedback)
                                    ? request.PlanFeedback
                                    : "No feedback provided. Please revise the plan and ask for approval again.";
                                bool approved = await InterpretPlanFeedbackAsync(feedback, cancellationToken);
                                if (approved)
                                {
                                    Console.WriteLine($"[LLM] User feedback interpreted as approval.");
                                    planReviewResponse = planReviewRequest.Approve();
                                    sessionState.WorkflowStage = WorkflowStage.PlanApproved;
                                }
                                else
                                {
                                    Console.WriteLine($"[LLM] User feedback interpreted as revision: {feedback}");
                                    planReviewResponse = planReviewRequest.Revise(feedback);
                                    sessionState.WorkflowStage = WorkflowStage.AwaitingPlanApproval;
                                }
                            }

                            AppendWorkflowHistory(conversationHistory, "Plan Review Response", planReviewResponse.ToString());
                            try { await logService.AppendLogAsync(new Models.LogEntry(request.OrchestrationId ?? string.Empty, DateTime.UtcNow, "PlanReviewResponse", planReviewResponse.ToString()), cancellationToken); } catch {}
                            sessionState.LatestPlanText = planReviewRequest.Plan.Text;
                            await run.SendResponseAsync(requestInfoEvent.Request.CreateResponse(planReviewResponse));
                            break;
                        }

                    case MagenticProgressLedgerUpdatedEvent progressUpdated:
                        latestProgressLedger = progressUpdated.ProgressLedger;
                        try { await logService.AppendLogAsync(new Models.LogEntry(request.OrchestrationId ?? string.Empty, DateTime.UtcNow, "Progress", FormatProgressLedger(latestProgressLedger)), cancellationToken); } catch {}
                        break;

                    case WorkflowOutputEvent outputEvent when outputEvent.Is<List<ChatMessage>>():
                        finalOutput = outputEvent;
                        try
                        {
                            var messages = outputEvent.As<List<ChatMessage>>();
                            var lastMessage = messages?.LastOrDefault(m => !string.IsNullOrWhiteSpace(m.Text));
                            if (lastMessage is not null)
                            {
                                await logService.AppendLogAsync(new Models.LogEntry(request.OrchestrationId ?? string.Empty, DateTime.UtcNow, "Output", lastMessage.Text), cancellationToken);
                            }
                        }
                        catch { }
                        break;

                    case WorkflowErrorEvent workflowError:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Error.WriteLine(workflowError.Exception?.ToString() ?? "Unknown workflow error.");
                        Console.ResetColor();
                        AppendWorkflowHistory(conversationHistory, "Workflow Error",
                            workflowError.Exception?.ToString() ?? "Unknown workflow error.");
                        try { await logService.AppendLogAsync(new Models.LogEntry(request.OrchestrationId ?? string.Empty, DateTime.UtcNow, "WorkflowError", workflowError.Exception?.ToString() ?? "Unknown workflow error."), cancellationToken); } catch {}
                        break;

                    case ExecutorFailedEvent executorFailed:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Error.WriteLine(
                            $"Executor '{executorFailed.ExecutorId}' failed: " +
                            (executorFailed.Data is null ? "unknown error" : executorFailed.Data.ToString()));
                        Console.ResetColor();
                        AppendWorkflowHistory(conversationHistory, "Executor Failed",
                            $"Executor '{executorFailed.ExecutorId}' failed.");
                        try { await logService.AppendLogAsync(new Models.LogEntry(request.OrchestrationId ?? string.Empty, DateTime.UtcNow, "ExecutorFailed", $"Executor '{executorFailed.ExecutorId}' failed."), cancellationToken); } catch {}
                        break;
                }

                sessionState.Messages = conversationHistory;
                conversationService.SaveConversationSession(request.OrchestrationId, sessionState);
            }

            // Flush the last agent's accumulated response
            if (currentAgentId is not null && currentAgentTextBuilder.Length > 0)
            {
                result.AgentResponses.Add(new AgentResponse
                {
                    AgentName = currentAgentId,
                    Message = currentAgentTextBuilder.ToString(),
                    Status = "Completed",
                    CompletedOnUtc = DateTime.UtcNow
                });
            }
        }

        if (finalOutput is not null)
        {
            var messages = finalOutput.As<List<ChatMessage>>();
            var lastMessage = messages?.LastOrDefault(m => !string.IsNullOrWhiteSpace(m.Text));
            result.FinalResponse = lastMessage?.Text;
            result.Status = "Completed";
        }

        result.CompletedOnUtc = DateTime.UtcNow;

        logger.LogInformation(
            "Completed orchestration {OrchestrationId} with {AgentCount} agent responses",
            result.OrchestrationId,
            result.AgentResponses.Count);

        return result;
    }

        private static void WriteMagenticMessage(string title, string? content)
    {
        Console.WriteLine();
        Console.WriteLine($"[Magentic {title}]");
        Console.WriteLine(content);
    }

    private static void AppendWorkflowHistory(List<ChatMessage> history, string source, string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;
        history.Add(new ChatMessage(ChatRole.System, $"[{source}] {content}") { AuthorName = source });
    }

    private static string FormatProgressLedger(MagenticProgressLedger ledger) =>
       string.Join(Environment.NewLine,
           $"Request satisfied: {ledger.IsRequestSatisfied}",
           $"In loop: {ledger.IsInLoop}",
           $"Making progress: {ledger.IsProgressBeingMade}",
           $"Next speaker: {ledger.NextSpeaker}",
           $"Instruction: {ledger.InstructionOrQuestion}");

    private static bool ApprovePlanIfNeeded(string? planText)
    {
        if (string.IsNullOrWhiteSpace(planText) || Console.IsInputRedirected || Console.IsOutputRedirected)
            return true;

        Console.WriteLine();
        Console.WriteLine("Plan verification required. Approve? [y/n]: ");
        while (true)
        {
            Console.Write("Approval [y/n]: ");
            string? response = Console.ReadLine();
            if (string.Equals(response, "y", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(response, "yes", StringComparison.OrdinalIgnoreCase))
            { Console.WriteLine("Plan approved."); Console.WriteLine(); return true; }

            if (string.Equals(response, "n", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(response, "no", StringComparison.OrdinalIgnoreCase))
            { Console.WriteLine("Plan rejected."); Console.WriteLine(); return false; }

            Console.WriteLine("Please enter 'y' or 'n'.");
        }
    } 

    private static List<PlannedTask> CreatePlannedTasks(
        MagenticOrchestrationRequest request,
        DateTime createdOnUtc)
    {
        return
        [
            CreatePlannedTask(request, "Receive orchestration request", "Receive and validate the incoming orchestration request.", createdOnUtc),
            CreatePlannedTask(request, "Read SPS order details", "Read SPS order details from the source system.", createdOnUtc),
            CreatePlannedTask(request, "Extract entities", "Extract entities from the SPS order and profile data.", createdOnUtc),
            CreatePlannedTask(request, "Prepare orchestration result", "Prepare the final orchestration response payload.", createdOnUtc)
        ];
    }

    private static PlannedTask CreatePlannedTask(
        MagenticOrchestrationRequest request,
        string taskName,
        string taskDescription,
        DateTime createdOnUtc)
    {
        return new PlannedTask
        {
            OrchestrationId = request.OrchestrationId ?? string.Empty,
            TaskName = taskName,
            TaskDescription = taskDescription,
            TaskStatus = "Planned",
            CreatedOnUtc = createdOnUtc,
            Request = request
        };
    }

    private async Task<bool> InterpretPlanFeedbackAsync(string feedback, CancellationToken cancellationToken)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System,
                """
                You are a plan-approval classifier. Given a user's free-text response to a proposed plan,
                determine whether the user is approving the plan or requesting changes.

                Reply with exactly one word: "approved" or "revision".
                - Reply "approved" if the user confirms, accepts, or approves the plan (e.g. "looks good", "go ahead", "approved", "yes", "proceed").
                - Reply "revision" if the user asks for changes, modifications, or expresses any concern (e.g. "change X", "add Y", "I don't like Z", "revise to...").
                """),
            new(ChatRole.User, feedback)
        };

        var response = await chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
        var answer = response.Text.Trim().ToLowerInvariant();

        logger.LogInformation("[PlanFeedbackClassifier] Feedback: '{Feedback}' → classified as: '{Answer}'", feedback, answer);

        return answer.Contains("approved");
    }

}
