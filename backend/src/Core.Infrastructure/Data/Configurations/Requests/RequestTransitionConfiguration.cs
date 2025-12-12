using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Infrastructure.Data.Configurations.Requests;

public sealed class RequestTransitionConfiguration : IEntityTypeConfiguration<RequestTransition>
{
    public void Configure(EntityTypeBuilder<RequestTransition> builder)
    {
        builder.ToTable("request_transitions", "requests");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .HasColumnType("uuid");

        builder.Property(t => t.RequestTypeId)
            .HasColumnName("request_type_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => new RequestTypeId(value));

        builder.Property(t => t.FromStatusCode)
            .HasColumnName("from_status_code")
            .HasColumnType("text")
            .IsRequired()
            .HasConversion(
                code => code.Value,
                value => new RequestStatusCode(value));

        builder.Property(t => t.ToStatusCode)
            .HasColumnName("to_status_code")
            .HasColumnType("text")
            .IsRequired()
            .HasConversion(
                code => code.Value,
                value => new RequestStatusCode(value));

        builder.Property(t => t.ActionCode)
            .HasColumnName("action_code")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(t => t.RequiredPermission)
            .HasColumnName("required_permission")
            .HasColumnType("text");

        builder.Property(t => t.IsEnabled)
            .HasColumnName("is_enabled")
            .IsRequired()
            .HasColumnType("boolean");

        builder.HasIndex(t => new { t.RequestTypeId, t.FromStatusCode, t.ActionCode })
            .IsUnique();
    }
}

