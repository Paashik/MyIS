using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Infrastructure.Data.Configurations.Mdm;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers", "mdm");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Code)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(s => s.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.FullName)
            .HasMaxLength(500);

        builder.Property(s => s.Inn)
            .HasMaxLength(20);

        builder.Property(s => s.Kpp)
            .HasMaxLength(20);

        builder.Property(s => s.Email)
            .HasMaxLength(100);

        builder.Property(s => s.Phone)
            .HasMaxLength(50);

        builder.Property(s => s.City)
            .HasMaxLength(50);

        builder.Property(s => s.Address)
            .HasMaxLength(250);

        builder.Property(s => s.Site)
            .HasMaxLength(200);

        builder.Property(s => s.Note)
            .HasMaxLength(1000);

        builder.Property(s => s.ProviderType);

        builder.Property(s => s.ExternalSystem)
            .HasMaxLength(50);

        builder.Property(s => s.ExternalId)
            .HasMaxLength(50);

        builder.Property(s => s.SyncedAt);

        builder.Property(s => s.IsActive)
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        builder.HasIndex(s => new { s.ExternalSystem, s.ExternalId })
            .IsUnique();

        builder.HasIndex(s => s.Code)
            .IsUnique()
            .HasFilter("\"Code\" IS NOT NULL");
    }
}
