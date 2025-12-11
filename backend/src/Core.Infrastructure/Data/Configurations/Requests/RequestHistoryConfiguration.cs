using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;
using MyIS.Core.Domain.Users;

namespace MyIS.Core.Infrastructure.Data.Configurations.Requests;

public class RequestHistoryConfiguration : IEntityTypeConfiguration<RequestHistory>
{
    public void Configure(EntityTypeBuilder<RequestHistory> builder)
    {
        builder.ToTable("request_history", "requests");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedOnAdd();

        builder.Property(h => h.RequestId)
            .HasColumnName("request_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => new RequestId(value));

        builder.Property(h => h.Action)
            .HasColumnName("action")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(h => h.PerformedBy)
            .HasColumnName("performed_by")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(h => h.Timestamp)
            .HasColumnName("timestamp")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(h => h.OldValue)
            .HasColumnName("old_value")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(h => h.NewValue)
            .HasColumnName("new_value")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(h => h.Comment)
            .HasColumnName("comment")
            .HasColumnType("text");

        builder.HasOne(h => h.Request)
            .WithMany(r => r.History)
            .HasForeignKey(h => h.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK на core.users.performed_by
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(h => h.PerformedBy)
            .HasPrincipalKey(u => u.Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}