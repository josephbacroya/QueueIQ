using QueueIQ.Shared.DTOs;

namespace QueueIQ.Shared.Interfaces;

/// <summary>
/// Strongly-typed SignalR client interface — defines what the server can invoke on connected clients.
/// 
/// Using a typed hub gives compile-time safety: if you rename a method here,
/// the compiler catches every call site. With untyped hubs, a typo in
/// Clients.Group("x").SendAsync("QueueUpdated", ...) silently fails at runtime.
/// 
/// Interview talking point: "I used a typed hub interface so the compiler
/// enforces correctness on all SignalR broadcasts — no stringly-typed method names."
/// </summary>
public interface IQueueHubClient
{
    /// <summary>
    /// Full queue refresh — broadcast to the business's SignalR group.
    /// Sends the complete current queue so clients always have a consistent snapshot.
    /// </summary>
    Task QueueUpdated(IEnumerable<TicketDto> queue);

    /// <summary>
    /// Targeted notification when a specific ticket is called.
    /// Sent to the business group so the customer's page can react.
    /// </summary>
    Task TicketCalled(TicketDto ticket);

    /// <summary>
    /// Targeted position update for a specific customer.
    /// Sent to the business group — each client filters by their own ticket ID.
    /// </summary>
    Task PositionUpdated(QueuePositionDto position);
}
