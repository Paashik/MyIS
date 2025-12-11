using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Infrastructure.Data.Configurations.Requests;

public class RequestTypeConfiguration : IEntityTypeConfiguration<RequestType>
{
    public void Configure(EntityTypeBuilder<RequestType> builder)
    {
        builder.ToTable("request_types", "requests");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedOnAdd()
            .HasConversion(
                id => id.Value,
                value => new RequestTypeId(value));

        builder.Property(t => t.Code)
            .HasColumnName("code")
            .IsRequired()
            .HasColumnType("text");

        builder.HasIndex(t => t.Code)
            .IsUnique();

        builder.Property(t => t.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasColumnType("text");

        builder.Property(t => t.Description)
            .HasColumnName("description")
            .HasColumnType("text");
    }
}