using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QueueIQ.Shared.DTOs;
using QueueIQ.Shared.Interfaces;

namespace QueueIQ.Api.Controllers;

/// <summary>
/// Queue operations — the core of QueueIQ.
/// 
/// Two audiences use these endpoints:
/// 1. Staff: call next, update status, view full queue
/// 2. Customers: join queue (via QR code), check position
/// 
/// Auth/role separation will be added in a later phase.
/// </summary>
[ApiController]
[Route("api")]
public class QueueController : ControllerBase
{
    private readonly IQueueService _queueService;
    private readonly IBusinessService _businessService;

    public QueueController(IQueueService queueService, IBusinessService businessService)
    {
        _queueService = queueService;
        _businessService = businessService;
    }

    /// <summary>Join the queue — called by customers via QR code/link.</summary>
    [HttpPost("businesses/{slug}/queue")]
    [EnableRateLimiting("QueueJoinLimiter")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> JoinQueue(string slug, [FromBody] CreateTicketDto dto)
    {
        var business = await _businessService.GetBySlugAsync(slug);
        if (business is null) return NotFound();

        var ticket = await _queueService.JoinQueueAsync(business.Id, dto);
        return CreatedAtAction(nameof(GetPosition), new { ticketId = ticket.Id }, ticket);
    }

    /// <summary>Get the current active queue for a business.</summary>
    [HttpGet("businesses/{slug}/queue")]
    [ProducesResponseType(typeof(IEnumerable<TicketDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQueue(string slug)
    {
        var business = await _businessService.GetBySlugAsync(slug);
        if (business is null) return NotFound();

        var queue = await _queueService.GetQueueAsync(business.Id);
        return Ok(queue);
    }

    /// <summary>Call the next waiting customer — staff action.</summary>
    [HttpPost("businesses/{slug}/queue/call-next")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CallNext(string slug)
    {
        var business = await _businessService.GetBySlugAsync(slug);
        if (business is null) return NotFound();

        var ticket = await _queueService.CallNextAsync(business.Id);
        return ticket is null ? NoContent() : Ok(ticket);
    }

    /// <summary>Update a ticket's status (e.g., Called → InService → Done).</summary>
    [HttpPatch("queue/tickets/{ticketId}/status")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateTicketStatus(Guid ticketId, [FromBody] UpdateTicketStatusDto dto)
    {
        var ticket = await _queueService.UpdateTicketStatusAsync(ticketId, dto);
        return Ok(ticket);
    }

    /// <summary>Get a customer's current position — lightweight, customer-facing.</summary>
    [HttpGet("queue/tickets/{ticketId}/position")]
    [ProducesResponseType(typeof(QueuePositionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPosition(Guid ticketId)
    {
        var position = await _queueService.GetPositionAsync(ticketId);
        return position is null ? NotFound() : Ok(position);
    }

    /// <summary>Get a customer's active ticket by their token to resume sessions.</summary>
    [HttpGet("businesses/{slug}/queue/active-ticket")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActiveTicketByToken(string slug, [FromQuery] string token)
    {
        var business = await _businessService.GetBySlugAsync(slug);
        if (business is null) return NotFound();

        var ticket = await _queueService.GetActiveTicketByTokenAsync(business.Id, token);
        return ticket is null ? NotFound() : Ok(ticket);
    }
}
