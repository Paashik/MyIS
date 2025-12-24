using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Infrastructure.Data.Configurations.Mdm;

public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("currencies", "mdm");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code)
            .HasMaxLength(10)
            ;

        builder.Property(c => c.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Symbol)
            .HasMaxLength(10);

        builder.Property(c => c.Rate)
            .HasPrecision(18, 4);

        builder.Property(c => c.IsActive)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        builder.HasIndex(c => c.Code)
            .IsUnique();

        
    }
}
