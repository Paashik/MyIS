using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Infrastructure.Data.Configurations.Mdm;

public class ItemSequenceConfiguration : IEntityTypeConfiguration<ItemSequence>
{
    public void Configure(EntityTypeBuilder<ItemSequence> builder)
    {
        builder.ToTable("item_sequences", "mdm");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.ItemKind)
            .IsRequired();

        builder.Property(s => s.Prefix)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(s => s.NextNumber)
            .IsRequired();

        builder.HasIndex(s => s.ItemKind)
            .IsUnique();
    }
}