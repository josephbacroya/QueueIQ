using Microsoft.EntityFrameworkCore;
using QueueIQ.Data.Entities;

namespace QueueIQ.Data;

/// <summary>
/// Central EF Core DbContext for QueueIQ.
/// Entity configurations are in separate IEntityTypeConfiguration classes
/// to keep this file clean as the model grows.
/// </summary>
public class QueueIQDbContext : DbContext
{
    public QueueIQDbContext(DbContextOptions<QueueIQDbContext> options)
        : base(options)
    {
    }

    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<ServiceType> ServiceTypes => Set<ServiceType>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<QueueSnapshot> QueueSnapshots => Set<QueueSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration implementations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QueueIQDbContext).Assembly);
    }
}
