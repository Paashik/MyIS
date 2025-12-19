using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Infrastructure.Data.Configurations.Mdm;

public sealed class CounterpartyExternalLinkConfiguration : IEntityTypeConfiguration<CounterpartyExternalLink>
{
    public void Configure(EntityTypeBuilder<CounterpartyExternalLink> builder)
    {
        builder.ToTable("counterparty_external_links", "mdm");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CounterpartyId)
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

        builder.HasIndex(x => new { x.ExternalSystem, x.ExternalEntity, x.ExternalId })
            .IsUnique();

        builder.HasIndex(x => x.CounterpartyId);
    }
}

