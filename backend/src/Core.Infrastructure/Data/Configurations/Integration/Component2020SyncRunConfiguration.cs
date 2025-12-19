using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Infrastructure.Data.Entities.Integration;

namespace MyIS.Core.Infrastructure.Data.Configurations.Integration;

public class Component2020SyncRunConfiguration : IEntityTypeConfiguration<Component2020SyncRun>
{
    public void Configure(EntityTypeBuilder<Component2020SyncRun> builder)
    {
        builder.ToTable("component2020_sync_run", "integration");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.StartedAt)
            .IsRequired();

        builder.Property(r => r.FinishedAt);

        builder.Property(r => r.StartedByUserId);

        builder.Property(r => r.Scope)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.Mode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.ProcessedCount)
            .IsRequired();

        builder.Property(r => r.ErrorCount)
            .IsRequired();

        builder.Property(r => r.CountersJson)
            .HasColumnType("jsonb");

        builder.Property(r => r.Summary)
            .HasMaxLength(1000);
    }
}