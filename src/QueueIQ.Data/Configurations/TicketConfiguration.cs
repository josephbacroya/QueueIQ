using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QueueIQ.Data.Entities;

namespace QueueIQ.Data.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.CustomerToken)
            .IsRequired()
            .HasMaxLength(100);

        // Store enum as string for readability in the database
        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.JoinedAt).IsRequired();
        builder.Property(t => t.CalledAt);
        builder.Property(t => t.CompletedAt);

        // ML prediction fields — nullable
        builder.Property(t => t.PredictedWaitMinutes);
        builder.Property(t => t.NoShowRiskScore);

        // Concurrency token — prevents two staff from calling the same "next" ticket
        // SQLite doesn't support native rowversion, so we configure it as a byte array concurrency token
        builder.Property(t => t.RowVersion)
            .IsConcurrencyToken();

        // Composite index: business + status — the most common query is
        // "get all Waiting tickets for business X, ordered by JoinedAt"
        builder.HasIndex(t => new { t.BusinessId, t.Status });

        // Index on CustomerToken for position lookups
        builder.HasIndex(t => t.CustomerToken);

        // FK to Business — cascade delete
        builder.HasOne(t => t.Business)
            .WithMany(b => b.Tickets)
            .HasForeignKey(t => t.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK to ServiceType — restrict delete (don't delete tickets if service type removed)
        builder.HasOne(t => t.ServiceType)
            .WithMany(s => s.Tickets)
            .HasForeignKey(t => t.ServiceTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
