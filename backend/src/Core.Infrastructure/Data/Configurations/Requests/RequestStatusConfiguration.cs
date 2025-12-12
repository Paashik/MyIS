using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Infrastructure.Data.Configurations.Requests;

public class RequestStatusConfiguration : IEntityTypeConfiguration<RequestStatus>
{
    public void Configure(EntityTypeBuilder<RequestStatus> builder)
    {
        builder.ToTable("request_statuses", "requests");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedOnAdd()
            .HasConversion(
                id => id.Value,
                value => new RequestStatusId(value));

        builder.Property(s => s.Code)
            .HasColumnName("code")
            .IsRequired()
            .HasConversion(
                code => code.Value,
                value => new RequestStatusCode(value))
            .HasColumnType("text");

        builder.HasIndex(s => s.Code)
            .IsUnique();

        builder.Property(s => s.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasColumnType("text");

        builder.Property(s => s.IsFinal)
            .HasColumnName("is_final")
            .IsRequired()
            .HasColumnType("boolean");

        builder.Property(s => s.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(s => s.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasColumnType("boolean");
    }
}
