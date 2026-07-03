using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using QueueIQ.Api.Exceptions;
using QueueIQ.Data;
using QueueIQ.Data.Entities;
using QueueIQ.Shared.DTOs;
using QueueIQ.Shared.Interfaces;

namespace QueueIQ.Api.Services;

/// <summary>
/// Business management service — handles CRUD for businesses and service types.
/// Includes slug generation and uniqueness enforcement.
/// </summary>
public partial class BusinessService : IBusinessService
{
    private readonly QueueIQDbContext _db;
    private readonly ILogger<BusinessService> _logger;

    public BusinessService(QueueIQDbContext db, ILogger<BusinessService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<BusinessDto?> GetBySlugAsync(string slug)
    {
        var business = await _db.Businesses
            .Include(b => b.ServiceTypes)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Slug == slug);

        return business is null ? null : MapToDto(business);
    }

    public async Task<BusinessDto> CreateAsync(CreateBusinessDto dto)
    {
        var slug = string.IsNullOrWhiteSpace(dto.Slug)
            ? GenerateSlug(dto.Name)
            : GenerateSlug(dto.Slug);

        // Ensure slug uniqueness
        if (await _db.Businesses.AnyAsync(b => b.Slug == slug))
        {
            throw new SlugConflictException(slug);
        }

        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Slug = slug,
            OwnerId = "placeholder-owner", // Will be replaced with Identity user ID
            TimeZoneId = dto.TimeZoneId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Businesses.Add(business);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created business '{Name}' with slug '{Slug}'", business.Name, business.Slug);

        return MapToDto(business);
    }

    public async Task<BusinessDto> UpdateAsync(string slug, UpdateBusinessDto dto)
    {
        var business = await _db.Businesses
            .Include(b => b.ServiceTypes)
            .FirstOrDefaultAsync(b => b.Slug == slug)
            ?? throw new NotFoundException("Business", slug);

        business.Name = dto.Name.Trim();

        if (!string.IsNullOrWhiteSpace(dto.Slug))
        {
            var newSlug = GenerateSlug(dto.Slug);
            if (newSlug != business.Slug && await _db.Businesses.AnyAsync(b => b.Slug == newSlug))
            {
                throw new SlugConflictException(newSlug);
            }
            business.Slug = newSlug;
        }

        if (!string.IsNullOrWhiteSpace(dto.TimeZoneId))
        {
            business.TimeZoneId = dto.TimeZoneId;
        }

        await _db.SaveChangesAsync();
        return MapToDto(business);
    }

    public async Task<ServiceTypeDto> AddServiceTypeAsync(Guid businessId, CreateServiceTypeDto dto)
    {
        var businessExists = await _db.Businesses.AnyAsync(b => b.Id == businessId);
        if (!businessExists)
        {
            throw new NotFoundException("Business", businessId);
        }

        var serviceType = new ServiceType
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Name = dto.Name.Trim(),
            AvgDurationMinutes = dto.AvgDurationMinutes
        };

        _db.ServiceTypes.Add(serviceType);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Added service type '{Name}' to business {BusinessId}", serviceType.Name, businessId);

        return new ServiceTypeDto(serviceType.Id, serviceType.Name, serviceType.AvgDurationMinutes);
    }

    public async Task<IEnumerable<ServiceTypeDto>> GetServiceTypesAsync(Guid businessId)
    {
        return await _db.ServiceTypes
            .Where(s => s.BusinessId == businessId)
            .AsNoTracking()
            .Select(s => new ServiceTypeDto(s.Id, s.Name, s.AvgDurationMinutes))
            .ToListAsync();
    }

    /// <summary>
    /// Converts a name/text into a URL-friendly slug.
    /// "Joe's Barbershop" → "joes-barbershop"
    /// </summary>
    private static string GenerateSlug(string text)
    {
        var slug = text.ToLowerInvariant().Trim();
        slug = SlugInvalidChars().Replace(slug, "");  // Remove non-alphanumeric (except spaces/hyphens)
        slug = SlugWhitespace().Replace(slug, "-");    // Replace spaces with hyphens
        slug = SlugMultipleHyphens().Replace(slug, "-"); // Collapse multiple hyphens
        slug = slug.Trim('-');
        return slug;
    }

    private static BusinessDto MapToDto(Business business) => new(
        business.Id,
        business.Name,
        business.Slug,
        business.TimeZoneId,
        business.CreatedAt,
        business.ServiceTypes.Select(s => new ServiceTypeDto(s.Id, s.Name, s.AvgDurationMinutes)).ToList()
    );

    [GeneratedRegex(@"[^a-z0-9\s-]")]
    private static partial Regex SlugInvalidChars();

    [GeneratedRegex(@"\s+")]
    private static partial Regex SlugWhitespace();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex SlugMultipleHyphens();
}
