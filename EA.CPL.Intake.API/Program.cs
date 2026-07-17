using Azure.AI.OpenAI;
using Azure.Identity;
using EA.CPL.Magentic.Orchestration.Abstractions;
using EA.CPL.Magentic.Orchestration.DependencyInjection;
using EA.CPL.Magentic.Orchestration.Models;
using EA.CPL.Magentic.Orchestration.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using OpenAI;
using Serilog;
using System.ClientModel.Primitives;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    // Configure the web application builder
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .WriteTo.Console());

    builder.Services.AddOpenApi();

    // Configure antiforgery cookie to be secure and same-site friendly for local dev (HTTPS required)
    builder.Services.AddAntiforgery(options =>
    {
        options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    });

    builder.Services.AddMagenticOrchestration(builder.Configuration);

    // Add CORS policy for local development so Blazor UI (different origin/port) can call the API and SignalR hub
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("LocalDev", policy => policy
            .WithOrigins(
                "https://localhost:7290",
                "https://localhost:7293",
                "https://localhost:7294",
                "http://localhost:5156",
                "http://localhost:5012",
                "http://localhost:5120")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
    });
    // SignalR and log hub
    builder.Services.AddSignalR();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    // Enable CORS for local development before mapping hubs/endpoints
    app.UseCors("LocalDev");

    // For development over HTTP, disable secure cookie requirement
    if (app.Environment.IsDevelopment())
    {
        app.UseCookiePolicy(new CookiePolicyOptions
        {
            Secure = CookieSecurePolicy.None,
            MinimumSameSitePolicy = SameSiteMode.Lax
        });
    }
    else
    {
        // Enforce secure cookies and SameSite policy in production (HTTPS only)
        app.UseCookiePolicy(new CookiePolicyOptions
        {
            Secure = CookieSecurePolicy.Always,
            MinimumSameSitePolicy = SameSiteMode.Lax
        });
    }

    // Map SignalR hub for logs
    app.MapHub<EA.CPL.Intake.API.Hubs.LogHub>("/hubs/logs");

    // Wire ILogService events to SignalR broadcasts
    var logService = app.Services.GetRequiredService<EA.CPL.Magentic.Orchestration.Abstractions.ILogService>();
    var hubContext = app.Services.GetRequiredService<Microsoft.AspNetCore.SignalR.IHubContext<EA.CPL.Intake.API.Hubs.LogHub>>();
    logService.LogAppended += async (entry) =>
    {
        try
        {
            // Use SendCoreAsync to avoid SDK mismatch with SendAsync extension
            await hubContext.Clients.Group(entry.OrchestrationId).SendCoreAsync("ReceiveLog", new object[] { entry });
        }
        catch { }
    };
    app.UseSerilogRequestLogging();

    // Only enforce HTTPS redirect in production
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.MapPost("/api/orchestrations/run", async (
        MagenticOrchestrationRequest request,
        IMagenticOrchestrator orchestrator,
        ILogger<Program> logger,
        EA.CPL.Magentic.Orchestration.Abstractions.ILogService logService,
        CancellationToken cancellationToken) =>
    {
        var validationErrors = new Dictionary<string, string[]>();

        //AddRequiredValidation(validationErrors, nameof(request.State), request.State);
        //AddRequiredValidation(validationErrors, nameof(request.County), request.County);
        //AddRequiredValidation(validationErrors, nameof(request.SourceSystem), request.SourceSystem);
        //AddRequiredValidation(validationErrors, nameof(request.SourceAccount), request.SourceAccount);
        //AddRequiredValidation(validationErrors, nameof(request.OrderNumber), request.OrderNumber);
        //AddRequiredValidation(validationErrors, nameof(request.OrderType), request.OrderType);

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        request.OrchestrationId = string.IsNullOrWhiteSpace(request.OrchestrationId)
            ? Guid.NewGuid().ToString("N")
            : request.OrchestrationId;

        logger.LogInformation(
            "Received orchestration request {OrchestrationId} for order {OrderNumber}",
            request.OrchestrationId,
            request.OrderNumber);

        try
        {
            // Emit initial log
            await logService.AppendLogAsync(new EA.CPL.Magentic.Orchestration.Models.LogEntry(
                request.OrchestrationId!, DateTime.UtcNow, "API", "Orchestration requested"), cancellationToken);

            var result = await orchestrator.RunAsync(request, cancellationToken);

            // Emit completion log
            await logService.AppendLogAsync(new EA.CPL.Magentic.Orchestration.Models.LogEntry(
                request.OrchestrationId!, DateTime.UtcNow, "API", $"Orchestration {result.Status}"), cancellationToken);

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "An unexpected error occurred while running orchestration {OrchestrationId}",
                request.OrchestrationId);
            return Results.Problem("An unexpected error occurred while running the orchestration.");
        }
    })
    .WithName("RunMagenticOrchestration");

    try
    {
        app.Run();
    }
    catch (System.IO.IOException ioEx) when (ioEx.Message.Contains("address already in use", StringComparison.OrdinalIgnoreCase) || ioEx.InnerException is System.Net.Sockets.SocketException)
    {
        // Provide a clearer runtime message when the configured port is already in use.
        Console.Error.WriteLine("Failed to start web host: the configured HTTP/S port is already in use.\n" +
            "Please stop the process using the port or change the application URL in Properties/launchSettings.json or set ASPNETCORE_URLS environment variable.");
        Console.Error.WriteLine(ioEx.ToString());
        Environment.Exit(1);
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "API host terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

//static void AddRequiredValidation(
//    IDictionary<string, string[]> validationErrors,
//    string fieldName,
//    string? value)
//{
//    if (!string.IsNullOrWhiteSpace(value))
//    {
//        return;
//    }

//    validationErrors[fieldName] = [$"The {fieldName} field is required."];
//}
