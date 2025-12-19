using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Infrastructure.Data.Configurations.Mdm;

public sealed class CounterpartyRoleConfiguration : IEntityTypeConfiguration<CounterpartyRole>
{
    public void Configure(EntityTypeBuilder<CounterpartyRole> builder)
    {
        builder.ToTable("counterparty_roles", "mdm");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CounterpartyId)
            .IsRequired();

        builder.Property(x => x.RoleType)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.CounterpartyId, x.RoleType })
            .IsUnique();
    }
}

