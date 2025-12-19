using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Infrastructure.Data.Configurations.Mdm;

public class ManufacturerConfiguration : IEntityTypeConfiguration<Manufacturer>
{
    public void Configure(EntityTypeBuilder<Manufacturer> builder)
    {
        builder.ToTable("manufacturers", "integration");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Code)
            .HasMaxLength(50)
            ;

        builder.Property(m => m.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.FullName)
            .HasMaxLength(200);

        builder.Property(m => m.Site)
            .HasMaxLength(100);

        builder.Property(m => m.Note)
            .HasMaxLength(500);

        builder.Property(m => m.IsActive)
            .IsRequired();

        builder.Property(m => m.ExternalSystem)
            .HasMaxLength(50);

        builder.Property(m => m.ExternalId)
            .HasMaxLength(50);

        builder.Property(m => m.SyncedAt);

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.Property(m => m.UpdatedAt)
            .IsRequired();

        builder.HasIndex(m => m.Code)
            .IsUnique();

        builder.HasIndex(m => new { m.ExternalSystem, m.ExternalId })
            .IsUnique();
    }
}
