using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Infrastructure.Data.Configurations.Integration;

public sealed class ExternalEntityLinkConfiguration : IEntityTypeConfiguration<ExternalEntityLink>
{
    public void Configure(EntityTypeBuilder<ExternalEntityLink> builder)
    {
        builder.ToTable("external_entity_links", "integration");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.EntityId)
            .IsRequired();

        builder.Property(x => x.ExternalSystem)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ExternalEntity)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ExternalId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.SourceType);

        builder.Property(x => x.SyncedAt);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.EntityType, x.ExternalSystem, x.ExternalEntity, x.ExternalId })
            .IsUnique();

        builder.HasIndex(x => new { x.EntityType, x.EntityId });
    }
}

