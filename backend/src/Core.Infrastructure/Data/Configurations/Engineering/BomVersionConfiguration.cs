using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Engineering.Entities;

namespace MyIS.Core.Infrastructure.Data.Configurations.Engineering;

public class BomVersionConfiguration : IEntityTypeConfiguration<BomVersion>
{
    public void Configure(EntityTypeBuilder<BomVersion> builder)
    {
        builder.ToTable("bom_versions", "engineering");

        builder.HasKey(bv => bv.Id);

        builder.Property(bv => bv.Id)
            .HasColumnName("id")
            .HasColumnType("uuid");

        builder.Property(bv => bv.ProductId)
            .HasColumnName("product_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(bv => bv.VersionCode)
            .HasColumnName("version_code")
            .IsRequired()
            .HasColumnType("text")
            .HasMaxLength(50);

        builder.Property(bv => bv.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasColumnType("integer")
            .HasConversion<int>();

        builder.Property(bv => bv.Source)
            .HasColumnName("source")
            .IsRequired()
            .HasColumnType("integer")
            .HasConversion<int>();

        builder.Property(bv => bv.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(bv => bv.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(bv => bv.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Relationships
        builder.HasOne(bv => bv.Product)
            .WithMany(p => p.BomVersions)
            .HasForeignKey(bv => bv.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(bv => bv.Lines)
            .WithOne(bl => bl.BomVersion)
            .HasForeignKey(bl => bl.BomVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(bv => bv.Operations)
            .WithOne(bo => bo.BomVersion)
            .HasForeignKey(bo => bo.BomVersionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}