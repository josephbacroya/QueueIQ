using Microsoft.AspNetCore.SignalR;
using QueueIQ.Shared.Interfaces;

namespace QueueIQ.Api.Hubs;

/// <summary>
/// The SignalR hub for real-time queue updates.
/// Clients connect to this hub to receive live updates for a specific business.
/// 
/// Interview talking point: "I used SignalR groups to isolate traffic in this multi-tenant
/// system. When a client joins the queue for 'joes-barbershop', they only subscribe to 
/// that group. This ensures they don't receive traffic for every other business on the platform."
/// </summary>
public class QueueHub : Hub<IQueueHubClient>
{
    private readonly ILogger<QueueHub> _logger;

    public QueueHub(ILogger<QueueHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called by the client when they navigate to a business's queue page.
    /// </summary>
    public async Task JoinBusinessQueue(string slug)
    {
        var groupName = GetGroupName(slug);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} joined group {GroupName}", Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Called by the client when they leave the queue page.
    /// </summary>
    public async Task LeaveBusinessQueue(string slug)
    {
        var groupName = GetGroupName(slug);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} left group {GroupName}", Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Standardized group naming convention.
    /// Using the slug because that's what the Blazor frontend will have in its URL
    /// before it even makes the first API call to get the BusinessId.
    /// </summary>
    public static string GetGroupName(string businessSlug) => $"queue-{businessSlug}";
}
