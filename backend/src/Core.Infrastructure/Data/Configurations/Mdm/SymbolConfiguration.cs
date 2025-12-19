using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Infrastructure.Data.Configurations.Mdm;

public class SymbolConfiguration : IEntityTypeConfiguration<Symbol>
{
    public void Configure(EntityTypeBuilder<Symbol> builder)
    {
        builder.ToTable("symbols", "integration");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.SymbolValue)
            .HasMaxLength(10);

        builder.Property(s => s.Photo)
            .HasMaxLength(255);

        builder.Property(s => s.LibraryPath)
            .HasMaxLength(255);

        builder.Property(s => s.LibraryRef)
            .HasMaxLength(100);

        builder.Property(s => s.IsActive)
            .IsRequired();

        builder.Property(s => s.ExternalSystem)
            .HasMaxLength(50);

        builder.Property(s => s.ExternalId)
            .HasMaxLength(50);

        builder.Property(s => s.SyncedAt);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        builder.HasIndex(s => s.Code)
            .IsUnique();

        builder.HasIndex(s => new { s.ExternalSystem, s.ExternalId })
            .IsUnique()
            .HasFilter("[ExternalSystem] IS NOT NULL AND [ExternalId] IS NOT NULL");
    }
}