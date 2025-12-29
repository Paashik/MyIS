using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Organization;

namespace MyIS.Core.Infrastructure.Data.Configurations.Organization;

public class OrgUnitContactConfiguration : IEntityTypeConfiguration<OrgUnitContact>
{
    public void Configure(EntityTypeBuilder<OrgUnitContact> builder)
    {
        builder.ToTable("org_unit_contacts", "org");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(x => x.OrgUnitId)
            .HasColumnName("org_unit_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(x => x.EmployeeId)
            .HasColumnName("employee_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(x => x.IncludeInRequest)
            .HasColumnName("include_in_request")
            .HasColumnType("boolean")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .HasColumnType("integer")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => x.OrgUnitId);
        builder.HasIndex(x => x.EmployeeId);

        builder.HasOne<OrgUnit>()
            .WithMany(o => o.Contacts)
            .HasForeignKey(x => x.OrgUnitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
