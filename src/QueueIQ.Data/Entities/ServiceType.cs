namespace QueueIQ.Data.Entities;

/// <summary>
/// Represents a type of service offered by a business (e.g., "Men's Haircut", "Oil Change").
/// AvgDurationMinutes is used as a baseline for wait-time estimation before ML is integrated.
/// </summary>
public class ServiceType
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AvgDurationMinutes { get; set; }

    // Navigation properties
    public Business Business { get; set; } = null!;
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
