using QueueIQ.Shared.Enums;

namespace QueueIQ.Data.Entities;

/// <summary>
/// Represents a customer's ticket in the queue.
/// This is the most critical entity — it tracks the full lifecycle from join to completion.
/// 
/// Key design decisions:
/// - CustomerToken is an anonymous identifier (not a user account) — customers don't need to register.
/// - RowVersion enables optimistic concurrency for "call next" operations (Phase 3 hardening).
/// - ML prediction fields are nullable — populated when the ML.NET model is integrated (Phase 5).
/// </summary>
public class Ticket
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid ServiceTypeId { get; set; }
    public string CustomerToken { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.Waiting;

    // Lifecycle timestamps
    public DateTime JoinedAt { get; set; }
    public DateTime? CalledAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // ML.NET outputs — nullable until Phase 5
    public double? PredictedWaitMinutes { get; set; }
    public double? NoShowRiskScore { get; set; }

    // Optimistic concurrency token — prevents double-calling in multi-device scenarios
    public byte[] RowVersion { get; set; } = null!;

    // Navigation properties
    public Business Business { get; set; } = null!;
    public ServiceType ServiceType { get; set; } = null!;
}
