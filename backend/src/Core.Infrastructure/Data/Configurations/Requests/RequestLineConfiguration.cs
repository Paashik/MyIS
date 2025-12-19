using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;
using MyIS.Core.Domain.Mdm.ValueObjects;

namespace MyIS.Core.Infrastructure.Data.Configurations.Requests;

public sealed class RequestLineConfiguration : IEntityTypeConfiguration<RequestLine>
{
    public void Configure(EntityTypeBuilder<RequestLine> builder)
    {
        builder.ToTable("request_lines", "requests");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasConversion(
                id => id.Value,
                value => new RequestLineId(value));

        builder.Property(l => l.RequestId)
            .HasColumnName("request_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => new RequestId(value));

        builder.Property(l => l.LineNo)
            .HasColumnName("line_no")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(l => l.ItemId)
            .HasColumnName("item_id")
            .HasColumnType("uuid")
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? ItemId.From(value.Value) : null);

        builder.Property(l => l.ExternalItemCode)
            .HasColumnName("external_item_code")
            .HasColumnType("text");

        builder.Property(l => l.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(l => l.Quantity)
            .HasColumnName("quantity")
            .HasColumnType("numeric")
            .IsRequired();

        builder.Property(l => l.UnitOfMeasureId)
            .HasColumnName("unit_of_measure_id")
            .HasColumnType("uuid");

        builder.Property(l => l.NeedByDate)
            .HasColumnName("need_by_date")
            .HasColumnType("timestamp with time zone");

        builder.Property(l => l.SupplierName)
            .HasColumnName("supplier_name")
            .HasColumnType("text");

        builder.Property(l => l.SupplierContact)
            .HasColumnName("supplier_contact")
            .HasColumnType("text");

        builder.Property(l => l.ExternalRowReferenceId)
            .HasColumnName("external_row_reference_id")
            .HasColumnType("text");

        builder.HasIndex(l => new { l.RequestId, l.LineNo })
            .IsUnique();
    }
}

