using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Infrastructure.Data.Configurations.Mdm;

public class TechnicalParameterConfiguration : IEntityTypeConfiguration<TechnicalParameter>
{
    public void Configure(EntityTypeBuilder<TechnicalParameter> builder)
    {
        builder.ToTable("technical_parameters", "integration");

        builder.HasKey(tp => tp.Id);

        builder.Property(tp => tp.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(tp => tp.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(tp => tp.Symbol)
            .HasMaxLength(20);

        builder.Property(tp => tp.UnitId);

        builder.Property(tp => tp.IsActive)
            .IsRequired();

        builder.Property(tp => tp.ExternalSystem)
            .HasMaxLength(50);

        builder.Property(tp => tp.ExternalId)
            .HasMaxLength(50);

        builder.Property(tp => tp.SyncedAt);

        builder.Property(tp => tp.CreatedAt)
            .IsRequired();

        builder.Property(tp => tp.UpdatedAt)
            .IsRequired();

        builder.HasIndex(tp => tp.Code)
            .IsUnique();

        builder.HasIndex(tp => new { tp.ExternalSystem, tp.ExternalId })
            .IsUnique()
            .HasFilter("[ExternalSystem] IS NOT NULL AND [ExternalId] IS NOT NULL");
    }
}