using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Infrastructure.Data.Configurations.Mdm;

public class ItemAttributeValueConfiguration : IEntityTypeConfiguration<ItemAttributeValue>
{
    public void Configure(EntityTypeBuilder<ItemAttributeValue> builder)
    {
        builder.ToTable("item_attribute_values", "mdm");

        builder.HasKey(av => new { av.ItemId, av.AttributeId });

        builder.Property(av => av.Value)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(av => av.CreatedAt)
            .IsRequired();

        builder.Property(av => av.UpdatedAt)
            .IsRequired();

        builder.HasOne(av => av.Item)
            .WithMany(i => i.AttributeValues)
            .HasForeignKey(av => av.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(av => av.Attribute)
            .WithMany()
            .HasForeignKey(av => av.AttributeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}