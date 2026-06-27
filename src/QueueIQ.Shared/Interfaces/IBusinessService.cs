using QueueIQ.Shared.DTOs;

namespace QueueIQ.Shared.Interfaces;

/// <summary>
/// Business management operations — CRUD for businesses and their service types.
/// </summary>
public interface IBusinessService
{
    Task<BusinessDto?> GetBySlugAsync(string slug);
    Task<BusinessDto> CreateAsync(CreateBusinessDto dto);
    Task<BusinessDto> UpdateAsync(string slug, UpdateBusinessDto dto);
    Task<ServiceTypeDto> AddServiceTypeAsync(Guid businessId, CreateServiceTypeDto dto);
    Task<IEnumerable<ServiceTypeDto>> GetServiceTypesAsync(Guid businessId);
}
