using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Infrastructure.Data.Entities.Integration;

namespace MyIS.Core.Infrastructure.Data.Configurations.Integration;

public class Component2020SyncCursorConfiguration : IEntityTypeConfiguration<Component2020SyncCursor>
{
    public void Configure(EntityTypeBuilder<Component2020SyncCursor> builder)
    {
        builder.ToTable("sync_cursors", "integration");

        builder.HasKey(c => new { c.ConnectionId, c.SourceEntity });

        builder.Property(c => c.ConnectionId)
            .IsRequired();

        builder.Property(c => c.SourceEntity)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.LastProcessedKey)
            .HasMaxLength(200);

        builder.Property(c => c.LastSyncAt)
            .IsRequired();
    }
}