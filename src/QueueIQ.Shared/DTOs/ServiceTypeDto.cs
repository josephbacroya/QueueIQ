namespace QueueIQ.Shared.DTOs;

/// <summary>
/// Read model for a service type offered by a business.
/// </summary>
public record ServiceTypeDto(
    Guid Id,
    string Name,
    int AvgDurationMinutes
);

/// <summary>
/// Write model for creating/updating a service type.
/// </summary>
public record CreateServiceTypeDto(
    string Name,
    int AvgDurationMinutes
);
