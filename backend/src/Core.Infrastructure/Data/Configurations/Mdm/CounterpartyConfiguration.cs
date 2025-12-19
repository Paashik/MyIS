using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Infrastructure.Data.Configurations.Mdm;

public sealed class CounterpartyConfiguration : IEntityTypeConfiguration<Counterparty>
{
    public void Configure(EntityTypeBuilder<Counterparty> builder)
    {
        builder.ToTable("counterparties", "mdm");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(50);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.FullName)
            .HasMaxLength(500);

        builder.Property(x => x.Inn)
            .HasMaxLength(20);

        builder.Property(x => x.Kpp)
            .HasMaxLength(20);

        builder.Property(x => x.Email)
            .HasMaxLength(100);

        builder.Property(x => x.Phone)
            .HasMaxLength(50);

        builder.Property(x => x.City)
            .HasMaxLength(50);

        builder.Property(x => x.Address)
            .HasMaxLength(250);

        builder.Property(x => x.Site)
            .HasMaxLength(200);

        builder.Property(x => x.SiteLogin)
            .HasMaxLength(100);

        builder.Property(x => x.SitePassword)
            .HasMaxLength(200);

        builder.Property(x => x.Note)
            .HasMaxLength(1000);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("\"Code\" IS NOT NULL");
    }
}
