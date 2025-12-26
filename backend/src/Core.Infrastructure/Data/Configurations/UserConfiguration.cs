using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Organization;
using MyIS.Core.Domain.Users;

namespace MyIS.Core.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(u => u.Login)
            .HasColumnName("login")
            .IsRequired()
            .HasColumnType("text");

        builder.HasIndex(u => u.Login)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .IsRequired()
            .HasColumnType("text");

        builder.Property(u => u.FullName)
            .HasColumnName("full_name")
            .HasColumnType("text");

        builder.Property(u => u.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasColumnType("boolean");

        builder.Property(u => u.EmployeeId)
            .HasColumnName("employee_id")
            .HasColumnType("uuid");

        builder.HasOne(u => u.Employee)
            .WithMany()
            .HasForeignKey(u => u.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => u.EmployeeId)
            .HasFilter("\"employee_id\" IS NOT NULL");

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
