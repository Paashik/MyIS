using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Organization;

namespace MyIS.Core.Infrastructure.Data.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees", "org");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(e => e.FullName)
            .HasColumnName("full_name")
            .IsRequired()
            .HasColumnType("text");

        builder.Property(e => e.ShortName)
            .HasColumnName("short_name")
            .IsRequired()
            .HasColumnType("text");

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .HasColumnType("text");

        builder.Property(e => e.Phone)
            .HasColumnName("phone")
            .HasColumnType("text");

        builder.Property(e => e.Notes)
            .HasColumnName("notes")
            .HasColumnType("text");

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasColumnType("boolean")
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired()
            .HasColumnType("timestamp with time zone");
    }
}

