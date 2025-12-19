using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Infrastructure.Data.Configurations.Mdm;

public class ParameterSetConfiguration : IEntityTypeConfiguration<ParameterSet>
{
    public void Configure(EntityTypeBuilder<ParameterSet> builder)
    {
        builder.ToTable("parameter_sets", "integration");

        builder.HasKey(ps => ps.Id);

        builder.Property(ps => ps.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(ps => ps.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(ps => ps.P0Id);

        builder.Property(ps => ps.P1Id);

        builder.Property(ps => ps.P2Id);

        builder.Property(ps => ps.P3Id);

        builder.Property(ps => ps.P4Id);

        builder.Property(ps => ps.P5Id);

        builder.Property(ps => ps.IsActive)
            .IsRequired();

        builder.Property(ps => ps.ExternalSystem)
            .HasMaxLength(50);

        builder.Property(ps => ps.ExternalId)
            .HasMaxLength(50);

        builder.Property(ps => ps.SyncedAt);

        builder.Property(ps => ps.CreatedAt)
            .IsRequired();

        builder.Property(ps => ps.UpdatedAt)
            .IsRequired();

        builder.HasIndex(ps => ps.Code)
            .IsUnique();

        builder.HasIndex(ps => new { ps.ExternalSystem, ps.ExternalId })
            .IsUnique()
            .HasFilter("[ExternalSystem] IS NOT NULL AND [ExternalId] IS NOT NULL");
    }
}