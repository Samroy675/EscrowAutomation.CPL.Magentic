using EA.CPL.Magentic.UI.Components;
using EA.CPL.Magentic.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure HttpClient with Intake API base URL from configuration
var intakeApiBaseUrl = builder.Configuration["IntakeApi:BaseUrl"] ?? "https://localhost:7293"; 
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(intakeApiBaseUrl), Timeout = TimeSpan.FromMinutes(10) });

// Register SignalRLogClient as scoped service using the configured base URL
builder.Services.AddScoped(sp => new EA.CPL.Magentic.UI.Services.SignalRLogClient($"{intakeApiBaseUrl}/hubs/logs"));

// Register LocalLogService as scoped service for storing logs to local file system
builder.Services.AddScoped<LocalLogService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}
else
{
    // In development, skip HTTPS redirect to allow HTTP connections
}


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
