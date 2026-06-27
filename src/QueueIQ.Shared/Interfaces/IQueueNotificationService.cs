using QueueIQ.Shared.DTOs;

namespace QueueIQ.Shared.Interfaces;

/// <summary>
/// A dedicated service layer abstraction for real-time notifications.
/// 
/// The QueueService calls this after any queue mutation. This decouples the 
/// core business logic from the SignalR implementation details.
/// 
/// Interview talking point: "Separating notification logic from business logic 
/// keeps the QueueService focused on data mutation and validation. If I later 
/// add webhook notifications or SMS alerts, I add them to this service without
/// touching the core queue logic."
/// </summary>
public interface IQueueNotificationService
{
    /// <summary>
    /// Broadcasts the fully updated queue to the business's group.
    /// Called when tickets are joined, status changes, etc.
    /// </summary>
    Task NotifyQueueUpdatedAsync(Guid businessId);

    /// <summary>
    /// Broadcasts a specific "ticket called" event to the business group.
    /// </summary>
    Task NotifyTicketCalledAsync(TicketDto ticket);

    /// <summary>
    /// Broadcasts a position update for a specific ticket.
    /// </summary>
    Task NotifyPositionUpdatedAsync(Guid ticketId);
}
