using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Infrastructure.Data.Entities.Integration;

namespace MyIS.Core.Infrastructure.Data.Configurations.Integration;

public class Component2020SyncScheduleConfiguration : IEntityTypeConfiguration<Component2020SyncSchedule>
{
    public void Configure(EntityTypeBuilder<Component2020SyncSchedule> builder)
    {
        builder.ToTable("component2020_sync_schedules", "integration");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.CronExpression)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.Scope)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.IsActive)
            .IsRequired();

        builder.Property(s => s.LastRunAt);

        builder.Property(s => s.NextRunAt);
    }
}