using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;
using MyIS.Core.Domain.Users;

namespace MyIS.Core.Infrastructure.Data.Configurations.Requests;

public class RequestConfiguration : IEntityTypeConfiguration<Request>
{
    public void Configure(EntityTypeBuilder<Request> builder)
    {
        builder.ToTable("requests", "requests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasConversion(
                id => id.Value,
                value => new RequestId(value));

        builder.Property(r => r.Title)
            .HasColumnName("title")
            .IsRequired()
            .HasColumnType("text");

        builder.Property(r => r.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(r => r.RequestTypeId)
            .HasColumnName("request_type_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => new RequestTypeId(value));

        builder.Property(r => r.RequestStatusId)
            .HasColumnName("request_status_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => new RequestStatusId(value));

        builder.Property(r => r.InitiatorId)
            .HasColumnName("initiator_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(r => r.RelatedEntityType)
            .HasColumnName("related_entity_type")
            .HasColumnType("text");

        builder.Property(r => r.RelatedEntityId)
            .HasColumnName("related_entity_id")
            .HasColumnType("uuid");

        builder.Property(r => r.ExternalReferenceId)
            .HasColumnName("external_reference_id")
            .HasColumnType("text");

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(r => r.DueDate)
            .HasColumnName("due_date")
            .HasColumnType("timestamp with time zone");

        // Relationships within requests schema
        builder.HasOne(r => r.Type)
            .WithMany()
            .HasForeignKey(r => r.RequestTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Status)
            .WithMany()
            .HasForeignKey(r => r.RequestStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK на core.users.initiator_id
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.InitiatorId)
            .HasPrincipalKey(u => u.Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}