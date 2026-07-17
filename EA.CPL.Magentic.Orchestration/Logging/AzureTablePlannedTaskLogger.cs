//using Azure.Data.Tables;
//using EA.CPL.Magentic.Orchestration.Abstractions;
//using EA.CPL.Magentic.Orchestration.Models;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;

//namespace EA.CPL.Magentic.Orchestration.Logging;

//public class AzureTablePlannedTaskLogger : IPlannedTaskLogger
//{
//    private readonly ILogger<AzureTablePlannedTaskLogger> _logger;
//    private readonly TableClient _tableClient;

//    public AzureTablePlannedTaskLogger(
//        IConfiguration configuration,
//        ILogger<AzureTablePlannedTaskLogger> logger)
//    {
//        _logger = logger;

//        var connectionString = configuration["AzureTableStorage:ConnectionString"];
//        if (string.IsNullOrWhiteSpace(connectionString))
//        {
//            throw new InvalidOperationException("Configuration value 'AzureTableStorage:ConnectionString' is required.");
//        }

//        var tableName = configuration["AzureTableStorage:PlannedTasksTableName"];
//        if (string.IsNullOrWhiteSpace(tableName))
//        {
//            throw new InvalidOperationException("Configuration value 'AzureTableStorage:PlannedTasksTableName' is required.");
//        }

//        _tableClient = new TableClient(connectionString, tableName);
//        _tableClient.CreateIfNotExists();
//    }

//    public async Task LogAsync(
//        PlannedTask plannedTask,
//        CancellationToken cancellationToken = default)
//    {
//        ArgumentNullException.ThrowIfNull(plannedTask);

//        var entity = new PlannedTaskEntity
//        {
//            PartitionKey = plannedTask.OrchestrationId,
//            RowKey = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}",
//            OrchestrationId = plannedTask.OrchestrationId,
//            TaskName = plannedTask.TaskName,
//            TaskDescription = plannedTask.TaskDescription,
//            TaskStatus = plannedTask.TaskStatus,
//            CreatedOnUtc = plannedTask.CreatedOnUtc,
//            State = plannedTask.Request?.State,
//            County = plannedTask.Request?.County,
//            Profile = plannedTask.Request?.Profile,
//            SourceSystem = plannedTask.Request?.SourceSystem,
//            SourceAccount = plannedTask.Request?.SourceAccount,
//            OrderNumber = plannedTask.Request?.OrderNumber,
//            OrderType = plannedTask.Request?.OrderType,
//            ProfileType = plannedTask.Request?.ProfileType
//        };

//        try
//        {
//            await _tableClient.AddEntityAsync(entity, cancellationToken);
//            _logger.LogInformation(
//                "Logged planned task {TaskName} for orchestration {OrchestrationId} to Azure Table Storage.",
//                plannedTask.TaskName,
//                plannedTask.OrchestrationId);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(
//                ex,
//                "Failed to log planned task {TaskName} for orchestration {OrchestrationId}.",
//                plannedTask.TaskName,
//                plannedTask.OrchestrationId);
//            throw;
//        }
//    }
//}
