using QueueIQ.Shared.Enums;

namespace QueueIQ.Shared.DTOs;

/// <summary>
/// Full read model for a ticket — used by staff on the dashboard.
/// Includes ML predictions (nullable until Phase 5).
/// </summary>
public record TicketDto(
    Guid Id,
    Guid BusinessId,
    Guid ServiceTypeId,
    string ServiceTypeName,
    string CustomerToken,
    TicketStatus Status,
    DateTime JoinedAt,
    DateTime? CalledAt,
    DateTime? CompletedAt,
    double? PredictedWaitMinutes,
    double? NoShowRiskScore,
    int Position
);

/// <summary>
/// Write model for a customer joining the queue.
/// CustomerToken is generated server-side if not provided.
/// </summary>
public record CreateTicketDto(
    Guid ServiceTypeId,
    string? CustomerToken = null
);

/// <summary>
/// Write model for staff updating a ticket's status.
/// </summary>
public record UpdateTicketStatusDto(
    TicketStatus NewStatus
);

/// <summary>
/// Lightweight model for customer-facing position updates.
/// Contains only what a waiting customer needs to see.
/// </summary>
public record QueuePositionDto(
    Guid TicketId,
    int Position,
    TicketStatus Status,
    double? EstimatedWaitMinutes,
    string ServiceTypeName
);
