# EA.CPL.Magentic Solution Documentation

## 1. Solution Overview

This workspace is a .NET 10 proof-of-concept for a Magentic orchestration flow.

The current solution contains:

- `EA.CPL.Intake.API` - Minimal API entry point
- `EA.CPL.Magentic.Orchestration` - Shared orchestration contracts, models, placeholder agents, orchestrator shell, Azure Table Storage logging, and Service Bus publishing support

The workspace also contains other files and folders, but only the projects above are currently included in the solution.

The code is intentionally simple and is designed as a skeleton. It includes:

- project structure
- shared request/result models
- interface contracts
- placeholder agent execution
- orchestration shell
- Serilog configuration
- Azure Table Storage planned-task logging
- minimal API endpoint shell

The actual orchestration intelligence, AI agent behavior, and production business rules are left as TODOs for later implementation.

---

## 2. Solution-Level Code Flow

The main runtime flow is:

1. A client sends a request to `EA.CPL.Intake.API`.
2. The API validates the request fields.
3. If `OrchestrationId` is missing, the API generates one.
4. The API calls `IMagenticOrchestrator.RunAsync(request)`.
5. The orchestrator creates a list of planned tasks.
6. Each planned task is logged to Azure Table Storage through `IPlannedTaskLogger`.
7. The orchestrator builds an execution context.
8. The orchestrator calls the placeholder agents.
9. The orchestrator returns a `MagenticOrchestrationResult`.

### Important execution notes

- The orchestrator currently performs a placeholder sequential flow.
- The agents return mock placeholder responses.
- The planned task logger writes to Azure Table Storage.
- Serilog is used for host and request logging.

---

## 3. Project Documentation

## 3.1 `EA.CPL.Intake.API`

This is the current Minimal API project used as the HTTP entry point.

### File: `EA.CPL.Intake.API/Program.cs`

This file contains the application bootstrap and the orchestration endpoint.

#### What goes into this file

- Serilog bootstrap setup
- `WebApplication` creation
- Serilog host registration
- OpenAPI registration
- dependency injection registration for orchestration services
- HTTPS redirection
- Serilog request logging middleware
- the orchestration POST endpoint
- exception handling for startup and runtime failures

#### Endpoint: `POST /api/orchestrations/run`

This endpoint:

- accepts `MagenticOrchestrationRequest`
- validates required fields
- generates `OrchestrationId` when missing
- logs the request
- calls `IMagenticOrchestrator.RunAsync`
- returns `Ok(result)`
- catches unexpected exceptions and returns `Problem`

#### Validation behavior

The endpoint validates these fields:

- `State`
- `County`
- `SourceSystem`
- `SourceAccount`
- `OrderNumber`
- `OrderType`

If any are missing, the endpoint returns a validation problem response.

#### File: `EA.CPL.Intake.API/EA.CPL.Intake.API.csproj`

This project file defines:

- target framework: `.NET 10`
- nullable reference types enabled
- implicit usings enabled
- package references for:
  - `Serilog`
  - `Serilog.AspNetCore`
  - `Serilog.Sinks.Console`
  - `Microsoft.AspNetCore.OpenApi`
  - `Microsoft.OpenApi`
- project reference to `EA.CPL.Magentic.Orchestration`

---

## 3.2 `EA.CPL.Magentic.Orchestration`

This is the shared orchestration library.

It contains the contracts, models, placeholder agents, orchestrator shell, Azure Table Storage logger, DI extension, and additional Service Bus/OpenAI support files.

### File structure

#### Core orchestration files

- `Abstractions/IMagenticOrchestrator.cs`
- `Abstractions/IAiAgent.cs`
- `Abstractions/IPlannedTaskLogger.cs`
- `Agents/SPSOrderReadAgent.cs`
- `Agents/EntityExtractionAgent.cs`
- `Orchestrators/MagenticOrchestrator.cs`
- `Models/MagenticOrchestrationRequest.cs`
- `Models/MagenticOrchestrationResult.cs`
- `Models/PlannedTask.cs`
- `Models/AgentResponse.cs`
- `Models/AgentExecutionContext.cs`
- `Logging/AzureTablePlannedTaskLogger.cs`
- `Logging/PlannedTaskEntity.cs`
- `DependencyInjection/ServiceCollectionExtensions.cs`

#### Additional support files currently in the project

- `Abstractions/IServiceBusPublisher.cs`
- `Models/JobMessage.cs`
- `Services/ServiceBusPublisher.cs`
- `Services/AzureOpenAI.cs`

These additional files are present in the project and should be documented as support or future-integration pieces. They are not part of the main request-to-orchestration flow.

### File: `Abstractions/IMagenticOrchestrator.cs`

Defines the orchestration contract.

#### What goes into this file

- a single method:
  - `Task<MagenticOrchestrationResult> RunAsync(MagenticOrchestrationRequest request, CancellationToken cancellationToken = default)`

This is the main entry point used by the API and the functions.

### File: `Abstractions/IAiAgent.cs`

Defines the agent contract.

#### What goes into this file

- `string Name { get; }`
- `Task<AgentResponse> ExecuteAsync(AgentExecutionContext context, CancellationToken cancellationToken = default)`

This allows each agent to be called in a consistent way.

### File: `Abstractions/IPlannedTaskLogger.cs`

Defines the planned-task logging contract.

#### What goes into this file

- `Task LogAsync(PlannedTask plannedTask, CancellationToken cancellationToken = default)`

The orchestrator uses this interface to persist planned tasks without knowing the storage details.

### File: `Abstractions/IServiceBusPublisher.cs`

This is a support abstraction for publishing messages to Service Bus.

#### What goes into this file

- `Task SendMessage(JobMessage message, CancellationToken ct = default)`

This interface is useful for future request/result dispatching.

### File: `Models/MagenticOrchestrationRequest.cs`

This is the request object shared by the API, functions, and orchestration layer.

#### Properties

- `OrchestrationId`
- `State`
- `County`
- `Profile`
- `SourceSystem`
- `SourceAccount`
- `OrderNumber`
- `OrderType`
- `ProfileType`

#### Purpose

Represents the inbound orchestration request data.

### File: `Models/MagenticOrchestrationResult.cs`

This is the response object returned after orchestration execution.

#### Properties

- `OrchestrationId`
- `Status`
- `FinalResponse`
- `List<AgentResponse> AgentResponses`
- `StartedOnUtc`
- `CompletedOnUtc`

#### Purpose

Represents the final orchestration output and any agent results.

### File: `Models/AgentResponse.cs`

Represents the result of a single agent execution.

#### Properties

- `AgentName`
- `Status`
- `Message`
- `Data`
- `CompletedOnUtc`

### File: `Models/PlannedTask.cs`

Represents a task that the orchestrator plans before execution.

#### Properties

- `OrchestrationId`
- `TaskName`
- `TaskDescription`
- `TaskStatus`
- `CreatedOnUtc`
- `Request`

### File: `Models/AgentExecutionContext.cs`

This is the runtime context passed into agents.

#### Properties

- `MagenticOrchestrationRequest Request`
- `Dictionary<string, object> Data`

### File: `Models/JobMessage.cs`

This is a support message contract used for Service Bus publishing scenarios.

#### What goes into this file

- `Subscribers` helper constants
- `JobMessage` properties such as:
  - `TargetSubscriber`
  - `MessageId`
  - `ConversationId`
  - `WorkflowId`
  - `TaskId`
  - `AgentName`
  - `TaskDescription`
  - `UserRequest`
  - `PreviousResults`
  - `RetryAttempt`
  - `MessageCreated`

This type supports future message-based workflows.

### File: `Agents/SPSOrderReadAgent.cs`

This is a placeholder agent for SPS order reading.

#### What goes into this file

- implements `IAiAgent`
- uses `ILogger<SPSOrderReadAgent>`
- returns agent name `SPSOrderRead`
- logs execution
- returns a placeholder `AgentResponse`
- includes TODO comments for the real SPS integration

#### Intended future responsibility

- read order details from SPS
- use:
  - `SourceSystem`
  - `SourceAccount`
  - `OrderNumber`
  - `OrderType`

### File: `Agents/EntityExtractionAgent.cs`

This is a placeholder agent for entity extraction.

#### What goes into this file

- implements `IAiAgent`
- uses `ILogger<EntityExtractionAgent>`
- returns agent name `EntityExtraction`
- logs execution
- returns a placeholder `AgentResponse`
- includes TODO comments for the real extraction logic

#### Intended future responsibility

- extract entities from profile/order data
- use:
  - `State`
  - `County`
  - `Profile`
  - `ProfileType`

### File: `Orchestrators/MagenticOrchestrator.cs`

This is the main orchestration shell.

#### What goes into this file

- implements `IMagenticOrchestrator`
- accepts:
  - `IPlannedTaskLogger`
  - `SPSOrderReadAgent`
  - `EntityExtractionAgent`
  - `ILogger<MagenticOrchestrator>`
- ensures `OrchestrationId` exists
- creates planned tasks
- logs each planned task through Azure Table Storage logger
- creates `AgentExecutionContext`
- executes the two placeholder agents sequentially
- returns `MagenticOrchestrationResult`
- contains TODO comment for future real orchestration logic

#### Planned tasks created by the orchestrator

1. Receive orchestration request
2. Read SPS order details
3. Extract entities
4. Prepare orchestration result

Each planned task includes:

- `OrchestrationId`
- `TaskName`
- `TaskDescription`
- `TaskStatus = "Planned"`
- `CreatedOnUtc`
- `Request`

#### Orchestrator flow in detail

1. Validate request is not null.
2. Generate `OrchestrationId` if missing.
3. Record `StartedOnUtc`.
4. Log that orchestration has started.
5. Build the planned task list.
6. Persist each planned task to Azure Table Storage.
7. Build the execution context.
8. Call `SPSOrderReadAgent.ExecuteAsync(...)`.
9. Call `EntityExtractionAgent.ExecuteAsync(...)`.
10. Create the final result object.
11. Log completion.
12. Return the result.

### File: `Logging/PlannedTaskEntity.cs`

This is the Azure Table Storage entity implementation.

#### What goes into this file

- implements `ITableEntity`
- includes:
  - `PartitionKey`
  - `RowKey`
  - `Timestamp`
  - `ETag`
  - `OrchestrationId`
  - `TaskName`
  - `TaskDescription`
  - `TaskStatus`
  - `CreatedOnUtc`
  - request detail fields:
    - `State`
    - `County`
    - `Profile`
    - `SourceSystem`
    - `SourceAccount`
    - `OrderNumber`
    - `OrderType`
    - `ProfileType`

#### Purpose

Stores each planned task as a row in Azure Table Storage.

### File: `Logging/AzureTablePlannedTaskLogger.cs`

This is the full Azure Table Storage logging implementation.

#### What goes into this file

- implements `IPlannedTaskLogger`
- accepts `IConfiguration` and `ILogger<AzureTablePlannedTaskLogger>`
- reads configuration values:
  - `AzureTableStorage:ConnectionString`
  - `AzureTableStorage:PlannedTasksTableName`
- throws `InvalidOperationException` if either value is missing
- creates a `TableClient`
- calls `CreateIfNotExists()`
- converts `PlannedTask` to `PlannedTaskEntity`
- uses:
  - `PartitionKey = OrchestrationId`
  - `RowKey = yyyyMMddHHmmssfff-GUID`
- writes to the table using `AddEntityAsync`
- logs success or failure

#### Logging behavior

- on success: logs that the planned task was written
- on failure: logs the exception and rethrows it

### File: `Services/ServiceBusPublisher.cs`

This is a support Service Bus publisher implementation.

#### What goes into this file

- implements `IServiceBusPublisher`
- uses `ServiceBusClient` and `ServiceBusSender`
- serializes `JobMessage` to JSON
- creates and sends a `ServiceBusMessage`
- logs message delivery

#### Purpose

This file supports future message publishing from orchestration or agent flows.

### File: `Services/AzureOpenAI.cs`

This is a support/future AI integration file.

#### What goes into this file

- environment-based configuration placeholders
- OpenAI endpoint, deployment, and API key placeholders
- current methods are stubs

#### Purpose

This file is not part of the main current execution path. It is a placeholder for future AI client setup.

### File: `DependencyInjection/ServiceCollectionExtensions.cs`

This file contains the DI registration entry point for the shared orchestration library.

#### What goes into this file

- static extension method:
  - `AddMagenticOrchestration(this IServiceCollection services, IConfiguration configuration)`
- registers:
  - `IMagenticOrchestrator` -> `MagenticOrchestrator`
  - `SPSOrderReadAgent`
  - `EntityExtractionAgent`
  - `IPlannedTaskLogger` -> `AzureTablePlannedTaskLogger`

#### Purpose

This gives each host project a single method to register the orchestration layer.

### File: `EA.CPL.Magentic.Orchestration/EA.CPL.Magentic.Orchestration.csproj`

This project file defines:

- target framework: `.NET 10`
- nullable reference types enabled
- implicit usings enabled
- package references for:
  - `Azure.AI.OpenAI`
  - `Azure.Data.Tables`
  - `Azure.Identity`
  - `Azure.Messaging.ServiceBus`
  - `Microsoft.Agents.AI`
  - `Microsoft.Agents.AI.Workflows`
  - `Microsoft.Extensions.AI`
  - `Microsoft.Extensions.Configuration.Abstractions`
  - `Microsoft.Extensions.DependencyInjection.Abstractions`
  - `Microsoft.Extensions.Logging.Abstractions`

---

## 3.3 `EA.CPL.Magentic.Functions.AgentRequestSubscriber`

This project is an Azure Functions isolated worker used for request-based Service Bus processing.

### File: `EA.CPL.Magentic.Functions.AgentRequestSubscriber/EA.CPL.Magentic.Functions.AgentRequestSubscriber.csproj`

#### What goes into this file

- `net10.0` target framework
- Azure Functions isolated worker settings
- package references for:
  - `Microsoft.Azure.Functions.Worker`
  - `Microsoft.Azure.Functions.Worker.Extensions.ServiceBus`
  - `Microsoft.Azure.Functions.Worker.Sdk`
  - `Microsoft.Extensions.Hosting`
  - `Serilog`
  - `Serilog.Extensions.Hosting`
  - `Serilog.Sinks.Console`
- project reference to `EA.CPL.Magentic.Orchestration`

### File: `EA.CPL.Magentic.Functions.AgentRequestSubscriber/Program.cs`

This is the isolated worker bootstrap file.

#### What goes into this file

- Serilog bootstrap logger
- `HostBuilder` creation
- `ConfigureFunctionsWorkerDefaults()`
- DI registration through `AddMagenticOrchestration(context.Configuration)`
- Serilog host configuration
- host run and exception handling

### File: `EA.CPL.Magentic.Functions.AgentRequestSubscriber/AgentRequestSubscriberFunction.cs`

This is the Service Bus trigger function.

#### What goes into this file

- `AgentRequestSubscriberFunction` class
- constructor injection for:
  - `IMagenticOrchestrator`
  - `ILogger<AgentRequestSubscriberFunction>`
- `RunAsync` method
- `ServiceBusTrigger` binding
- JSON deserialization into `MagenticOrchestrationRequest`
- `OrchestrationId` generation if missing
- call to `orchestrator.RunAsync(request, cancellationToken)`
- logging of the result
- TODO comments for future pre/post processing

#### Message flow

1. Service Bus message arrives.
2. Message is logged.
3. The message is deserialized.
4. `OrchestrationId` is ensured.
5. The orchestrator is called.
6. The result is logged.

### File: `EA.CPL.Magentic.Functions.AgentRequestSubscriber/local.settings.json`

This file provides local runtime values for development.

#### Keys included

- `AzureWebJobsStorage`
- `FUNCTIONS_WORKER_RUNTIME`
- `ServiceBus:ConnectionString`
- `ServiceBus:AgentRequestTopicName`
- `ServiceBus:AgentRequestSubscriptionName`
- `AzureTableStorage:ConnectionString`
- `AzureTableStorage:PlannedTasksTableName`

### File: `EA.CPL.Magentic.Functions.AgentRequestSubscriber/host.json`

This contains the minimal Functions host configuration.

---

## 3.4 `EA.CPL.Magentic.Functions.AgentResultSubscriber`

This project is an Azure Functions isolated worker used for result/completion message processing.

### File: `EA.CPL.Magentic.Functions.AgentResultSubscriber/EA.CPL.Magentic.Functions.AgentResultSubscriber.csproj`

#### What goes into this file

- `net10.0` target framework
- Azure Functions isolated worker settings
- package references for:
  - `Microsoft.Azure.Functions.Worker`
  - `Microsoft.Azure.Functions.Worker.Extensions.ServiceBus`
  - `Microsoft.Azure.Functions.Worker.Sdk`
  - `Microsoft.Extensions.Hosting`
  - `Serilog`
  - `Serilog.Extensions.Hosting`
  - `Serilog.Sinks.Console`
- project reference to `EA.CPL.Magentic.Orchestration`

### File: `EA.CPL.Magentic.Functions.AgentResultSubscriber/Program.cs`

This is the isolated worker bootstrap file.

#### What goes into this file

- Serilog bootstrap logger
- `HostBuilder` creation
- `ConfigureFunctionsWorkerDefaults()`
- DI registration through `AddMagenticOrchestration(context.Configuration)`
- Serilog host configuration
- host run and exception handling

### File: `EA.CPL.Magentic.Functions.AgentResultSubscriber/AgentResultSubscriberFunction.cs`

This is the Service Bus trigger function.

#### What goes into this file

- `AgentResultSubscriberFunction` class
- constructor injection for:
  - `IMagenticOrchestrator`
  - `ILogger<AgentResultSubscriberFunction>`
- `RunAsync` method
- `ServiceBusTrigger` binding
- JSON deserialization into `MagenticOrchestrationRequest` for now
- `OrchestrationId` generation if missing
- call to `orchestrator.RunAsync(request, cancellationToken)`
- logging of the result
- TODO comments for future result payload handling

#### Message flow

1. Service Bus result message arrives.
2. Message is logged.
3. The message is deserialized.
4. `OrchestrationId` is ensured.
5. The orchestrator is called.
6. The result is logged.

### File: `EA.CPL.Magentic.Functions.AgentResultSubscriber/local.settings.json`

This file provides local runtime values for development.

#### Keys included

- `AzureWebJobsStorage`
- `FUNCTIONS_WORKER_RUNTIME`
- `ServiceBus:ConnectionString`
- `ServiceBus:AgentResultTopicName`
- `ServiceBus:AgentResultSubscriptionName`
- `AzureTableStorage:ConnectionString`
- `AzureTableStorage:PlannedTasksTableName`

### File: `EA.CPL.Magentic.Functions.AgentResultSubscriber/host.json`

This contains the minimal Functions host configuration.

---

## 4. Legacy / Non-Solution Workspace Content

### `Core/EA.CPL.Magentic.Orchestrator`

The workspace may still contain an older `Core/EA.CPL.Magentic.Orchestrator` folder from previous iterations.

It is not currently included in the solution and is not part of the active runtime flow documented here.

If needed later, it can be retired, renamed, or aligned with the newer orchestration project.

---

## 5. Configuration Summary

### API configuration

- `AzureTableStorage:ConnectionString`
- `AzureTableStorage:PlannedTasksTableName`
- Serilog settings under `Serilog`

### Function configuration

- `ServiceBus:ConnectionString`
- `ServiceBus:AgentRequestTopicName`
- `ServiceBus:AgentRequestSubscriptionName`
- `ServiceBus:AgentResultTopicName`
- `ServiceBus:AgentResultSubscriptionName`
- `AzureTableStorage:ConnectionString`
- `AzureTableStorage:PlannedTasksTableName`

### Storage behavior

The logger currently expects Azure Table Storage to be available, and it uses:

- `UseDevelopmentStorage=true` for local development
- a table named `MagenticPlannedTasks` by default

---

## 6. Current POC Behavior vs Future Work

### What is implemented now

- solution structure
- shared contracts and models
- placeholder orchestration flow
- placeholder agents
- Azure Table logging for planned tasks
- Serilog bootstrap logging
- API endpoint shell
- Service Bus function shells
- Service Bus publishing support scaffolding
- Azure OpenAI support scaffolding

### What is intentionally not implemented yet

- real orchestration decision logic
- real AI agent logic
- real SPS integration
- real entity extraction logic
- real agent result processing logic
- advanced workflow/state management

These are left as TODOs so the POC can be extended without rewriting the project foundation.

---

## 7. Quick Reference: Which Code Goes Where

### API

- `Program.cs` - host bootstrap, DI, endpoint, validation, logging
- `appsettings.json` - configuration
- `appsettings.Development.json` - local overrides

### Orchestration library

- `Abstractions/*` - interfaces
- `Models/*` - request/result/context/task data models
- `Agents/*` - placeholder agents
- `Orchestrators/MagenticOrchestrator.cs` - orchestration skeleton
- `Logging/*` - Azure Table Storage implementation and table entity
- `DependencyInjection/*` - service registration
- `Services/*` - future publishing and AI support helpers

### Request subscriber function project

- `Program.cs` - isolated worker bootstrap
- `AgentRequestSubscriberFunction.cs` - Service Bus trigger shell
- `local.settings.json` - local runtime settings
- `host.json` - Functions host config

### Result subscriber function project

- `Program.cs` - isolated worker bootstrap
- `AgentResultSubscriberFunction.cs` - Service Bus trigger shell
- `local.settings.json` - local runtime settings
- `host.json` - Functions host config

---

## 8. End-to-End Execution Summary

### API-driven execution

1. Client sends request to API.
2. API validates request.
3. API assigns `OrchestrationId` if needed.
4. API calls orchestrator.
5. Orchestrator creates planned tasks.
6. Planned tasks are logged to Azure Table Storage.
7. Orchestrator executes placeholder agents.
8. Orchestrator returns a result.
9. API returns the result to the client.

### Function-driven execution

1. Service Bus message arrives.
2. Function deserializes the message.
3. Function ensures `OrchestrationId`.
4. Function calls orchestrator.
5. Orchestrator performs the same placeholder workflow.
6. Function logs the result.

---

## 9. Notes for Future Implementation

When the real business logic is added later, the following areas are expected to change first:

- `SPSOrderReadAgent`
- `EntityExtractionAgent`
- `MagenticOrchestrator`
- `ServiceBusPublisher`
- `AzureOpenAI`
- function payload contracts
- result handling in the subscriber functions
- planned task status updates beyond `Planned`

The current structure is intentionally built to support those later changes with minimal disruption.
