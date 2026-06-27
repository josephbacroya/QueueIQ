using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QueueIQ.Data.Entities;

namespace QueueIQ.Data.Configurations;

public class ServiceTypeConfiguration : IEntityTypeConfiguration<ServiceType>
{
    public void Configure(EntityTypeBuilder<ServiceType> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.AvgDurationMinutes)
            .IsRequired();

        // FK to Business — cascade delete (if business is removed, its services go too)
        builder.HasOne(s => s.Business)
            .WithMany(b => b.ServiceTypes)
            .HasForeignKey(s => s.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
