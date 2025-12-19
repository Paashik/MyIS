using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Infrastructure.Data.Configurations.Mdm;

public class BodyTypeConfiguration : IEntityTypeConfiguration<BodyType>
{
    public void Configure(EntityTypeBuilder<BodyType> builder)
    {
        builder.ToTable("body_types", "integration");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(b => b.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(b => b.Description)
            .HasMaxLength(500);

        builder.Property(b => b.Pins);

        builder.Property(b => b.Smt);

        builder.Property(b => b.Photo)
            .HasMaxLength(255);

        builder.Property(b => b.FootPrintPath)
            .HasMaxLength(255);

        builder.Property(b => b.FootprintRef)
            .HasMaxLength(100);

        builder.Property(b => b.FootprintRef2)
            .HasMaxLength(100);

        builder.Property(b => b.FootPrintRef3)
            .HasMaxLength(100);

        builder.Property(b => b.IsActive)
            .IsRequired();

        builder.Property(b => b.ExternalSystem)
            .HasMaxLength(50);

        builder.Property(b => b.ExternalId)
            .HasMaxLength(50);

        builder.Property(b => b.SyncedAt);

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.Property(b => b.UpdatedAt)
            .IsRequired();

        builder.HasIndex(b => b.Code)
            .IsUnique();

        builder.HasIndex(b => new { b.ExternalSystem, b.ExternalId })
            .IsUnique()
            .HasFilter("[ExternalSystem] IS NOT NULL AND [ExternalId] IS NOT NULL");
    }
}