using Microsoft.AspNetCore.Mvc;
using QueueIQ.Shared.DTOs;
using QueueIQ.Shared.Interfaces;

namespace QueueIQ.Api.Controllers;

/// <summary>
/// Business management endpoints — CRUD for businesses and service types.
/// Auth will be added in a later phase (only business owners should modify their business).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BusinessesController : ControllerBase
{
    private readonly IBusinessService _businessService;

    public BusinessesController(IBusinessService businessService)
    {
        _businessService = businessService;
    }

    /// <summary>Get a business by its slug (e.g., "joes-barbershop").</summary>
    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(BusinessDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var business = await _businessService.GetBySlugAsync(slug);
        return business is null ? NotFound() : Ok(business);
    }

    /// <summary>Create a new business.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(BusinessDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateBusinessDto dto)
    {
        var business = await _businessService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetBySlug), new { slug = business.Slug }, business);
    }

    /// <summary>Update an existing business.</summary>
    [HttpPut("{slug}")]
    [ProducesResponseType(typeof(BusinessDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string slug, [FromBody] UpdateBusinessDto dto)
    {
        var business = await _businessService.UpdateAsync(slug, dto);
        return Ok(business);
    }

    /// <summary>Add a service type to a business.</summary>
    [HttpPost("{slug}/service-types")]
    [ProducesResponseType(typeof(ServiceTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddServiceType(string slug, [FromBody] CreateServiceTypeDto dto)
    {
        var business = await _businessService.GetBySlugAsync(slug);
        if (business is null) return NotFound();

        var serviceType = await _businessService.AddServiceTypeAsync(business.Id, dto);
        return Created($"api/businesses/{slug}/service-types", serviceType);
    }

    /// <summary>Get all service types for a business.</summary>
    [HttpGet("{slug}/service-types")]
    [ProducesResponseType(typeof(IEnumerable<ServiceTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetServiceTypes(string slug)
    {
        var business = await _businessService.GetBySlugAsync(slug);
        if (business is null) return NotFound();

        var serviceTypes = await _businessService.GetServiceTypesAsync(business.Id);
        return Ok(serviceTypes);
    }
}
