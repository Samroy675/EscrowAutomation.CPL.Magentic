using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace EA.CPL.Magentic.Orchestration.Models;

public struct Subscribers
{
    public const char Delimiter = '|';
    public const string Log = "log";
    public const string SPSOrderRead = "spsorderread";
    public const string EntityExtraction = "entityextraction";
}

public class JobMessage
{
    public string City { get; set; }= string.Empty;
    public string? Country { get; set; }
    public const string ServiceBusURL = "ServiceBusURL";

    public const string Topic = "eacplmagentic";
    public string TargetSubscriber { get; set; }=string.Empty;
    public string MessageId { get; init; } = Guid.NewGuid().ToString();
    public string ConversationId { get; init; } = string.Empty;
    public string WorkflowId { get; init; } = string.Empty;
    public string TaskId { get; init; } = string.Empty;
    public string AgentName { get; init; } = string.Empty;
    public string TaskDescription { get; init; } = string.Empty;
    public DateTimeOffset MessageCreated { get; init; } = DateTimeOffset.UtcNow;

    // SPS Order Read fields
    public string? OrderNumber { get; set; }
    public string? SourceSystem { get; set; }
    public string? SourceAccount { get; set; }
    public string? Profile { get; set; }

    public string UserRequest { get; init; } = string.Empty;
    public string? PreviousResults { get; init; }
    public int RetryAttempt { get; init; }

}