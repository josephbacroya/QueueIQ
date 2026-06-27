namespace QueueIQ.Shared.Enums;

/// <summary>
/// Represents the lifecycle states of a queue ticket.
/// Transitions are validated server-side — not every transition is legal.
/// Valid: Waiting → Called → InService → Done/NoShow
/// </summary>
public enum TicketStatus
{
    Waiting,
    Called,
    InService,
    Done,
    NoShow
}
