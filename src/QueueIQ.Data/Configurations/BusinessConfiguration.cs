using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QueueIQ.Data.Entities;

namespace QueueIQ.Data.Configurations;

public class BusinessConfiguration : IEntityTypeConfiguration<Business>
{
    public void Configure(EntityTypeBuilder<Business> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.Slug)
            .IsRequired()
            .HasMaxLength(200);

        // Unique index on Slug — used for customer-facing URLs
        builder.HasIndex(b => b.Slug)
            .IsUnique();

        builder.Property(b => b.OwnerId)
            .IsRequired()
            .HasMaxLength(450); // ASP.NET Identity default key length

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        // Relationships defined from the child side in their own configurations
    }
}
