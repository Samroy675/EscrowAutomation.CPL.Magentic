using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using EA.CPL.Magentic.Orchestration.Models;

namespace EA.CPL.Magentic.UI.Services;

/// <summary>
/// Represents a client for receiving log entries via SignalR.
/// start the connection and subscribe to a specific orchestration ID to receive log entries in real-time.
/// </summary>
public class SignalRLogClient : IAsyncDisposable
{
    private readonly HubConnection _connection;

    public event Action<LogEntry>? LogReceived;
    
    public SignalRLogClient(string hubUrl)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<LogEntry>("ReceiveLog", entry => LogReceived?.Invoke(entry));
    }

    public async Task StartAsync()
    {
        if (_connection.State == HubConnectionState.Disconnected)
        {
            await _connection.StartAsync();
        }
    }

    public async Task SubscribeAsync(string orchestrationId)
    {
        if (string.IsNullOrEmpty(orchestrationId)) return;
        await StartAsync();
        await _connection.InvokeAsync("SubscribeToOrchestration", orchestrationId);
    }

    public async Task UnsubscribeAsync(string orchestrationId)
    {
        if (string.IsNullOrEmpty(orchestrationId)) return;
        try { await _connection.InvokeAsync("UnsubscribeFromOrchestration", orchestrationId); } catch { }
    }

    public async ValueTask DisposeAsync()
    {
        try { await _connection.StopAsync(); } catch { }
        await _connection.DisposeAsync();
    }
}
