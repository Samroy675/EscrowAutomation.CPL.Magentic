using Microsoft.AspNetCore.SignalR;

namespace EA.CPL.Intake.API.Hubs;

public class LogHub : Hub
{
    public async Task SubscribeToOrchestration(string orchestrationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, orchestrationId);
    }

    public async Task UnsubscribeFromOrchestration(string orchestrationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, orchestrationId);
    }
}
