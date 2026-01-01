using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Engineering.Entities;

namespace MyIS.Core.Infrastructure.Data.Configurations.Engineering;

public class BomOperationConfiguration : IEntityTypeConfiguration<BomOperation>
{
    public void Configure(EntityTypeBuilder<BomOperation> builder)
    {
        builder.ToTable("bom_operations", "engineering");

        builder.HasKey(bo => bo.Id);

        builder.Property(bo => bo.Id)
            .HasColumnName("id")
            .HasColumnType("uuid");

        builder.Property(bo => bo.BomVersionId)
            .HasColumnName("bom_version_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(bo => bo.Code)
            .HasColumnName("code")
            .IsRequired()
            .HasColumnType("text")
            .HasMaxLength(10);

        builder.Property(bo => bo.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasColumnType("text")
            .HasMaxLength(200);

        builder.Property(bo => bo.AreaName)
            .HasColumnName("area_name")
            .HasColumnType("text")
            .HasMaxLength(100);

        builder.Property(bo => bo.DurationMinutes)
            .HasColumnName("duration_minutes")
            .HasColumnType("integer");

        builder.Property(bo => bo.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasColumnType("integer")
            .HasConversion<int>();

        builder.Property(bo => bo.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        // Relationships
        builder.HasOne(bo => bo.BomVersion)
            .WithMany(bv => bv.Operations)
            .HasForeignKey(bo => bo.BomVersionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}