using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QueueIQ.Data.Entities;

namespace QueueIQ.Data.Configurations;

public class QueueSnapshotConfiguration : IEntityTypeConfiguration<QueueSnapshot>
{
    public void Configure(EntityTypeBuilder<QueueSnapshot> builder)
    {
        builder.HasKey(q => q.Id);

        builder.Property(q => q.Timestamp).IsRequired();
        builder.Property(q => q.QueueLength).IsRequired();
        builder.Property(q => q.StaffOnDuty).IsRequired();

        // Index for time-range queries (analytics dashboards, ML training data export)
        builder.HasIndex(q => new { q.BusinessId, q.Timestamp });

        // FK to Business
        builder.HasOne(q => q.Business)
            .WithMany(b => b.Snapshots)
            .HasForeignKey(q => q.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
