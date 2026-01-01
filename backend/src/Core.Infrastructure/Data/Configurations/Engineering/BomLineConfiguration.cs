using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Engineering.Entities;

namespace MyIS.Core.Infrastructure.Data.Configurations.Engineering;

public class BomLineConfiguration : IEntityTypeConfiguration<BomLine>
{
    public void Configure(EntityTypeBuilder<BomLine> builder)
    {
        builder.ToTable("bom_lines", "engineering");

        builder.HasKey(bl => bl.Id);

        builder.Property(bl => bl.Id)
            .HasColumnName("id")
            .HasColumnType("uuid");

        builder.Property(bl => bl.BomVersionId)
            .HasColumnName("bom_version_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(bl => bl.ParentItemId)
            .HasColumnName("parent_item_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(bl => bl.ItemId)
            .HasColumnName("item_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(bl => bl.Role)
            .HasColumnName("role")
            .IsRequired()
            .HasColumnType("integer")
            .HasConversion<int>();

        builder.Property(bl => bl.Quantity)
            .HasColumnName("quantity")
            .IsRequired()
            .HasColumnType("numeric(18,6)");

        builder.Property(bl => bl.UnitOfMeasure)
            .HasColumnName("unit_of_measure")
            .HasColumnType("text")
            .HasMaxLength(20);

        builder.Property(bl => bl.PositionNo)
            .HasColumnName("position_no")
            .HasColumnType("text")
            .HasMaxLength(50);

        builder.Property(bl => bl.Notes)
            .HasColumnName("notes")
            .HasColumnType("text");

        builder.Property(bl => bl.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasColumnType("integer")
            .HasConversion<int>();

        // Relationships
        builder.HasOne(bl => bl.BomVersion)
            .WithMany(bv => bv.Lines)
            .HasForeignKey(bl => bl.BomVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK to mdm.items for parent_item_id
        builder.HasOne<MyIS.Core.Domain.Mdm.Entities.Item>()
            .WithMany()
            .HasForeignKey(bl => bl.ParentItemId)
            .HasPrincipalKey(i => i.Id)
            .OnDelete(DeleteBehavior.Restrict);

        // FK to mdm.items for item_id
        builder.HasOne<MyIS.Core.Domain.Mdm.Entities.Item>()
            .WithMany()
            .HasForeignKey(bl => bl.ItemId)
            .HasPrincipalKey(i => i.Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}