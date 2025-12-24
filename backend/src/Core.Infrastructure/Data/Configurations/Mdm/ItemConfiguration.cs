using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Infrastructure.Data.Configurations.Mdm;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("items", "mdm");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(i => i.NomenclatureNo)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(i => i.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(i => i.ItemKind)
            .IsRequired();

        builder.Property(i => i.UnitOfMeasureId)
            .IsRequired();

        builder.Property(i => i.IsActive)
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .IsRequired();

        builder.Property(i => i.UpdatedAt)
            .IsRequired();

        builder.Property(i => i.IsEskd)
            .IsRequired();

        builder.Property(i => i.IsEskdDocument);

        builder.Property(i => i.Designation)
            .HasMaxLength(100);

        builder.Property(i => i.ManufacturerPartNumber)
            .HasMaxLength(100);

        builder.Property(i => i.ItemGroupId);

        builder.HasOne(i => i.UnitOfMeasure)
            .WithMany()
            .HasForeignKey(i => i.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.ItemGroup)
            .WithMany()
            .HasForeignKey(i => i.ItemGroupId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(i => i.AttributeValues)
            .WithOne(av => av.Item)
            .HasForeignKey(av => av.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => i.Code)
            .IsUnique();

        builder.HasIndex(i => i.NomenclatureNo)
            .IsUnique();

        
    }
}
