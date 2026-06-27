namespace QueueIQ.Shared.DTOs;

/// <summary>
/// Read-only representation of a business returned from the API.
/// Never exposes navigation properties or internal EF tracking data.
/// </summary>
public record BusinessDto(
    Guid Id,
    string Name,
    string Slug,
    DateTime CreatedAt,
    List<ServiceTypeDto> ServiceTypes
);

/// <summary>
/// Write model for creating a new business.
/// Slug is optional — auto-generated from Name if not provided.
/// </summary>
public record CreateBusinessDto(
    string Name,
    string? Slug = null
);

/// <summary>
/// Write model for updating a business.
/// </summary>
public record UpdateBusinessDto(
    string Name,
    string? Slug = null
);
