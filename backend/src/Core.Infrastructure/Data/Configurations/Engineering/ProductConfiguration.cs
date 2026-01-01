using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Engineering.Entities;

namespace MyIS.Core.Infrastructure.Data.Configurations.Engineering;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products", "engineering");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasColumnType("uuid");

        builder.Property(p => p.Code)
            .HasColumnName("code")
            .IsRequired()
            .HasColumnType("text")
            .HasMaxLength(50);

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasColumnType("text")
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(p => p.Type)
            .HasColumnName("type")
            .IsRequired()
            .HasColumnType("integer")
            .HasConversion<int>();

        builder.Property(p => p.ItemId)
            .HasColumnName("item_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Relationships
        builder.HasMany(p => p.BomVersions)
            .WithOne(bv => bv.Product)
            .HasForeignKey(bv => bv.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK to mdm.items
        builder.HasOne<MyIS.Core.Domain.Mdm.Entities.Item>()
            .WithMany()
            .HasForeignKey(p => p.ItemId)
            .HasPrincipalKey(i => i.Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}