namespace QueueIQ.Data.Entities;

/// <summary>
/// Represents a business (barbershop, clinic, repair shop, etc.) using QueueIQ.
/// Each business has a unique slug for customer-facing URLs (e.g., /join/joes-barbershop).
/// OwnerId will link to ASP.NET Core Identity when auth is added.
/// </summary>
public class Business
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty; // FK → AspNetUsers (Identity, Phase later)
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<ServiceType> ServiceTypes { get; set; } = new List<ServiceType>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<QueueSnapshot> Snapshots { get; set; } = new List<QueueSnapshot>();
}
