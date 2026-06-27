using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QueueIQ.Api.Hubs;
using QueueIQ.Data;
using QueueIQ.Shared.DTOs;
using QueueIQ.Shared.Interfaces;

namespace QueueIQ.Api.Services;

/// <summary>
/// Implementation of the notification service that pushes live updates via SignalR.
/// 
/// Note on dependencies: We inject QueueIQDbContext directly rather than IQueueService 
/// to avoid a circular dependency, since QueueService injects this notification service.
/// </summary>
public class QueueNotificationService : IQueueNotificationService
{
    private readonly IHubContext<QueueHub, IQueueHubClient> _hubContext;
    private readonly QueueIQDbContext _db;
    private readonly ILogger<QueueNotificationService> _logger;

    public QueueNotificationService(
        IHubContext<QueueHub, IQueueHubClient> hubContext,
        QueueIQDbContext db,
        ILogger<QueueNotificationService> logger)
    {
        _hubContext = hubContext;
        _db = db;
        _logger = logger;
    }

    public async Task NotifyQueueUpdatedAsync(Guid businessId)
    {
        // 1. Get the business slug to find the right SignalR group
        var slug = await _db.Businesses
            .Where(b => b.Id == businessId)
            .Select(b => b.Slug)
            .FirstOrDefaultAsync();

        if (slug is null) return;

        // 2. We need the full queue state to broadcast.
        // We replicate the minimal logic from QueueService.GetQueueAsync here to avoid circular DI.
        // For a larger app, we might use MediatR or a shared internal read repository.
        var activeStatuses = new[] { 
            QueueIQ.Shared.Enums.TicketStatus.Waiting, 
            QueueIQ.Shared.Enums.TicketStatus.Called, 
            QueueIQ.Shared.Enums.TicketStatus.InService 
        };

        var tickets = await _db.Tickets
            .Include(t => t.ServiceType)
            .Where(t => t.BusinessId == businessId && activeStatuses.Contains(t.Status))
            .OrderBy(t => t.Status == QueueIQ.Shared.Enums.TicketStatus.Waiting ? 0 : t.Status == QueueIQ.Shared.Enums.TicketStatus.Called ? 1 : 2)
            .ThenBy(t => t.JoinedAt)
            .AsNoTracking()
            .ToListAsync();

        var waitingTickets = tickets
            .Where(t => t.Status == QueueIQ.Shared.Enums.TicketStatus.Waiting)
            .OrderBy(t => t.JoinedAt)
            .ToList();

        var queueDtos = tickets.Select(t =>
        {
            var position = t.Status == QueueIQ.Shared.Enums.TicketStatus.Waiting
                ? waitingTickets.IndexOf(t) + 1
                : 0;
            return new TicketDto(
                t.Id, t.BusinessId, t.ServiceTypeId, t.ServiceType.Name, t.CustomerToken,
                t.Status, t.JoinedAt, t.CalledAt, t.CompletedAt, t.PredictedWaitMinutes, t.NoShowRiskScore, position);
        }).ToList();

        // 3. Broadcast to the group
        var groupName = QueueHub.GetGroupName(slug);
        await _hubContext.Clients.Group(groupName).QueueUpdated(queueDtos);
        
        _logger.LogInformation("Broadcast full queue update to group {GroupName}", groupName);
    }

    public async Task NotifyTicketCalledAsync(TicketDto ticket)
    {
        var slug = await GetSlugAsync(ticket.BusinessId);
        if (slug is null) return;

        var groupName = QueueHub.GetGroupName(slug);
        await _hubContext.Clients.Group(groupName).TicketCalled(ticket);
    }

    public async Task NotifyPositionUpdatedAsync(Guid ticketId)
    {
        // For now, full queue updates handle everything cleanly on the frontend.
        // This is a placeholder for more granular targeted notifications if needed.
        await Task.CompletedTask;
    }

    private async Task<string?> GetSlugAsync(Guid businessId)
    {
        return await _db.Businesses
            .Where(b => b.Id == businessId)
            .Select(b => b.Slug)
            .FirstOrDefaultAsync();
    }
}
