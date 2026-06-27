namespace QueueIQ.Data.Entities;

/// <summary>
/// Point-in-time snapshot of queue state — used for analytics and ML training data.
/// The ML.NET trainer (Phase 5) will use historical snapshots + completed tickets
/// to learn patterns in wait times and no-show rates.
/// </summary>
public class QueueSnapshot
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public DateTime Timestamp { get; set; }
    public int QueueLength { get; set; }
    public int StaffOnDuty { get; set; }

    // Navigation
    public Business Business { get; set; } = null!;
}
