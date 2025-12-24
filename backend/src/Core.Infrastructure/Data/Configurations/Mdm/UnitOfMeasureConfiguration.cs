using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Infrastructure.Data.Configurations.Mdm;

public class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        builder.ToTable("unit_of_measures", "mdm");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Code)
            .HasMaxLength(10)
            .IsRequired(false);

        builder.Property(u => u.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.Symbol)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(u => u.IsActive)
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .IsRequired();

        builder.HasIndex(u => u.Code)
            .IsUnique();

        builder.HasIndex(u => u.Name)
            .IsUnique();

        
    }
}
