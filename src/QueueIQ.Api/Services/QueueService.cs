using Microsoft.EntityFrameworkCore;
using QueueIQ.Api.Exceptions;
using QueueIQ.Data;
using QueueIQ.Data.Entities;
using QueueIQ.Shared.DTOs;
using QueueIQ.Shared.Enums;
using QueueIQ.Shared.Interfaces;

namespace QueueIQ.Api.Services;

/// <summary>
/// Core queue operations — the heart of QueueIQ.
/// 
/// Key design decisions:
/// - Status transitions are validated via a whitelist (not a switch/case cascade)
/// - Position is calculated at query time, not stored (avoids stale data)
/// - Async all the way down — no .Result or .Wait() blocking calls
/// 
/// Interview talking point: "The QueueService enforces a state machine for ticket
/// transitions. Invalid transitions throw a domain exception caught by middleware,
/// so controllers don't need to duplicate validation logic."
/// </summary>
public class QueueService : IQueueService
{
    private readonly QueueIQDbContext _db;
    private readonly IQueueNotificationService _notificationService;
    private readonly IPredictionService _predictionService;
    private readonly ILogger<QueueService> _logger;

    /// <summary>
    /// Valid state transitions — the ticket state machine.
    /// Only transitions listed here are allowed; anything else throws InvalidStatusTransitionException.
    /// </summary>
    private static readonly Dictionary<TicketStatus, HashSet<TicketStatus>> ValidTransitions = new()
    {
        [TicketStatus.Waiting] = [TicketStatus.Called, TicketStatus.NoShow],
        [TicketStatus.Called] = [TicketStatus.InService, TicketStatus.NoShow],
        [TicketStatus.InService] = [TicketStatus.Done, TicketStatus.NoShow],
        [TicketStatus.Done] = [],     // Terminal state
        [TicketStatus.NoShow] = [],   // Terminal state
    };

    public QueueService(
        QueueIQDbContext db,
        IQueueNotificationService notificationService,
        IPredictionService predictionService,
        ILogger<QueueService> logger)
    {
        _db = db;
        _notificationService = notificationService;
        _predictionService = predictionService;
        _logger = logger;
    }

    public async Task<TicketDto> JoinQueueAsync(Guid businessId, CreateTicketDto dto)
    {
        // Validate business exists
        var business = await _db.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == businessId)
            ?? throw new NotFoundException("Business", businessId);

        // Validate service type exists and belongs to this business
        var serviceType = await _db.ServiceTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == dto.ServiceTypeId && s.BusinessId == businessId)
            ?? throw new NotFoundException("ServiceType", dto.ServiceTypeId);

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            ServiceTypeId = dto.ServiceTypeId,
            CustomerToken = dto.CustomerToken ?? Guid.NewGuid().ToString("N")[..12], // Short, random token
            Status = TicketStatus.Waiting,
            JoinedAt = DateTime.UtcNow,
            RowVersion = Guid.NewGuid().ToByteArray() // Initialize concurrency token
        };

        // Calculate position (how many Waiting tickets are ahead of this one)
        var position = await CalculatePositionAsync(ticket);

        // Get count of staff on duty for this business (assume 3 for now, ideally queried from staff roster)
        int staffOnDuty = 3;

        // Populate ML predictions
        var predictions = await _predictionService.PredictAsync(
            serviceType.Name,
            serviceType.AvgDurationMinutes,
            position - 1, // Queue length BEFORE this ticket joined
            staffOnDuty);
            
        ticket.PredictedWaitMinutes = predictions.PredictedWaitMinutes;
        ticket.NoShowRiskScore = predictions.NoShowRiskScore;

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Ticket {TicketId} created for business {BusinessId}, service '{ServiceName}'. Wait: {Wait:F1}m, Risk: {Risk:P1}",
            ticket.Id, businessId, serviceType.Name, ticket.PredictedWaitMinutes, ticket.NoShowRiskScore);
        
        // Notify clients that the queue has updated
        await _notificationService.NotifyQueueUpdatedAsync(businessId);

        return MapToDto(ticket, serviceType.Name, position);
    }

    public async Task<TicketDto?> CallNextAsync(Guid businessId)
    {
        int maxRetries = 3;
        for (int i = 0; i < maxRetries; i++)
        {
            // Get the oldest Waiting ticket for this business
            var nextTicket = await _db.Tickets
                .Include(t => t.ServiceType)
                .Where(t => t.BusinessId == businessId && t.Status == TicketStatus.Waiting)
                .OrderBy(t => t.JoinedAt)
                .FirstOrDefaultAsync();

            if (nextTicket is null)
            {
                return null; // No one waiting
            }

            nextTicket.Status = TicketStatus.Called;
            nextTicket.CalledAt = DateTime.UtcNow;
            nextTicket.RowVersion = Guid.NewGuid().ToByteArray(); // Manually bump RowVersion for concurrency

            try
            {
                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "Ticket {TicketId} called for business {BusinessId}", nextTicket.Id, businessId);

                var dto = MapToDto(nextTicket, nextTicket.ServiceType.Name, 0);

                // Notify that a specific ticket was called, then update the full queue
                await _notificationService.NotifyTicketCalledAsync(dto);
                await _notificationService.NotifyQueueUpdatedAsync(businessId);

                return dto;
            }
            catch (DbUpdateConcurrencyException)
            {
                // The ticket was grabbed by someone else (high contention).
                // Discard tracked changes and try again to get the next person in line.
                _db.Entry(nextTicket).State = EntityState.Detached;
                _logger.LogWarning("Concurrency conflict calling next ticket for business {BusinessId}. Retrying...", businessId);
                continue;
            }
        }

        throw new ConcurrencyConflictException("Failed to call next customer after multiple attempts due to high contention.");
    }

    public async Task<TicketDto> UpdateTicketStatusAsync(Guid ticketId, UpdateTicketStatusDto dto)
    {
        var ticket = await _db.Tickets
            .Include(t => t.ServiceType)
            .FirstOrDefaultAsync(t => t.Id == ticketId)
            ?? throw new NotFoundException("Ticket", ticketId);

        // Validate the transition using the state machine
        if (!ValidTransitions.TryGetValue(ticket.Status, out var allowedNextStates) ||
            !allowedNextStates.Contains(dto.NewStatus))
        {
            throw new InvalidStatusTransitionException(
                ticket.Status.ToString(), dto.NewStatus.ToString());
        }

        ticket.Status = dto.NewStatus;

        // Set lifecycle timestamps based on the new status
        switch (dto.NewStatus)
        {
            case TicketStatus.Called:
                ticket.CalledAt = DateTime.UtcNow;
                break;
            case TicketStatus.Done:
            case TicketStatus.NoShow:
                ticket.CompletedAt = DateTime.UtcNow;
                break;
        }

        ticket.RowVersion = Guid.NewGuid().ToByteArray(); // Manually bump RowVersion for concurrency

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException();
        }

        _logger.LogInformation(
            "Ticket {TicketId} status changed to {Status}", ticketId, dto.NewStatus);

        var position = await CalculatePositionAsync(ticket);
        
        await _notificationService.NotifyQueueUpdatedAsync(ticket.BusinessId);

        return MapToDto(ticket, ticket.ServiceType.Name, position);
    }

    public async Task<IEnumerable<TicketDto>> GetQueueAsync(Guid businessId)
    {
        var activeStatuses = new[] { TicketStatus.Waiting, TicketStatus.Called, TicketStatus.InService };

        var tickets = await _db.Tickets
            .Include(t => t.ServiceType)
            .Where(t => t.BusinessId == businessId && activeStatuses.Contains(t.Status))
            .OrderBy(t => t.Status == TicketStatus.Waiting ? 0 : t.Status == TicketStatus.Called ? 1 : 2)
            .ThenBy(t => t.JoinedAt)
            .AsNoTracking()
            .ToListAsync();

        // Calculate positions for waiting tickets
        var waitingTickets = tickets
            .Where(t => t.Status == TicketStatus.Waiting)
            .OrderBy(t => t.JoinedAt)
            .ToList();

        return tickets.Select(t =>
        {
            var position = t.Status == TicketStatus.Waiting
                ? waitingTickets.IndexOf(t) + 1
                : 0;
            return MapToDto(t, t.ServiceType.Name, position);
        });
    }

    public async Task<QueuePositionDto?> GetPositionAsync(Guid ticketId)
    {
        var ticket = await _db.Tickets
            .Include(t => t.ServiceType)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket is null) return null;

        var position = await CalculatePositionAsync(ticket);

        // Use the ML.NET prediction from the ticket
        double? estimatedWait = ticket.PredictedWaitMinutes;

        return new QueuePositionDto(
            ticket.Id,
            position,
            ticket.Status,
            estimatedWait,
            ticket.ServiceType.Name
        );
    }

    /// <summary>
    /// Calculates position in queue: count of Waiting tickets that joined before this one.
    /// Position 1 = you're next. Position 0 = you've been called or are being served.
    /// </summary>
    private async Task<int> CalculatePositionAsync(Ticket ticket)
    {
        if (ticket.Status != TicketStatus.Waiting) return 0;

        return await _db.Tickets
            .CountAsync(t =>
                t.BusinessId == ticket.BusinessId &&
                t.Status == TicketStatus.Waiting &&
                t.JoinedAt < ticket.JoinedAt) + 1;
    }

    private static TicketDto MapToDto(Ticket ticket, string serviceTypeName, int position) => new(
        ticket.Id,
        ticket.BusinessId,
        ticket.ServiceTypeId,
        serviceTypeName,
        ticket.CustomerToken,
        ticket.Status,
        ticket.JoinedAt,
        ticket.CalledAt,
        ticket.CompletedAt,
        ticket.PredictedWaitMinutes,
        ticket.NoShowRiskScore,
        position
    );
}
