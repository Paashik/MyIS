using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Infrastructure.Data.Entities.Integration;

namespace MyIS.Core.Infrastructure.Data.Configurations.Integration;

public class Component2020SyncErrorConfiguration : IEntityTypeConfiguration<Component2020SyncError>
{
    public void Configure(EntityTypeBuilder<Component2020SyncError> builder)
    {
        builder.ToTable("component2020_sync_error", "integration");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SyncRunId)
            .IsRequired();

        builder.Property(e => e.EntityType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.ExternalEntity)
            .HasMaxLength(100);

        builder.Property(e => e.ExternalKey)
            .HasMaxLength(200);

        builder.Property(e => e.Message)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(e => e.Details)
            .HasColumnType("text");

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasOne(e => e.SyncRun)
            .WithMany(r => r.Errors)
            .HasForeignKey(e => e.SyncRunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}