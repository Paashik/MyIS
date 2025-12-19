using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Infrastructure.Data.Entities.Integration;

namespace MyIS.Core.Infrastructure.Data.Configurations.Integration;

public class Component2020ConnectionConfiguration : IEntityTypeConfiguration<Component2020Connection>
{
    public void Configure(EntityTypeBuilder<Component2020Connection> builder)
    {
        builder.ToTable("component2020_connection", "integration");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.MdbPath)
            .HasMaxLength(500);

        builder.Property(c => c.Login)
            .HasMaxLength(100);

        builder.Property(c => c.EncryptedPassword)
            .HasMaxLength(500);

        builder.Property(c => c.IsActive)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        builder.Property(c => c.LastTestedAt);

        builder.Property(c => c.LastTestMessage)
            .HasMaxLength(1000);
    }
}