using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Statuses.Entities;

namespace MyIS.Core.Infrastructure.Data.Configurations.Statuses;

public sealed class StatusConfiguration : IEntityTypeConfiguration<Status>
{
    public void Configure(EntityTypeBuilder<Status> builder)
    {
        builder.ToTable("statuses", "mdm");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.GroupId);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.Color);

        builder.Property(x => x.Flags);

        builder.Property(x => x.SortOrder);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.HasOne<Status>()
            .WithMany()
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.GroupId, x.Name });
    }
}
