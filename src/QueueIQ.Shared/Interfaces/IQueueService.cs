using QueueIQ.Shared.DTOs;

namespace QueueIQ.Shared.Interfaces;

/// <summary>
/// Core queue operations — this is the heart of QueueIQ.
/// Registered in DI as a scoped service (one instance per HTTP request).
/// </summary>
public interface IQueueService
{
    /// <summary>Join the queue — called by customers via QR code/link.</summary>
    Task<TicketDto> JoinQueueAsync(Guid businessId, CreateTicketDto dto);

    /// <summary>Call the next waiting customer — called by staff.</summary>
    Task<TicketDto?> CallNextAsync(Guid businessId);

    /// <summary>Update a ticket's status (e.g., Called → InService → Done).</summary>
    Task<TicketDto> UpdateTicketStatusAsync(Guid ticketId, UpdateTicketStatusDto dto);

    /// <summary>Get all active tickets for a business's queue.</summary>
    Task<IEnumerable<TicketDto>> GetQueueAsync(Guid businessId);

    /// <summary>Get a customer's current position — lightweight, customer-facing.</summary>
    Task<QueuePositionDto?> GetPositionAsync(Guid ticketId);
}
