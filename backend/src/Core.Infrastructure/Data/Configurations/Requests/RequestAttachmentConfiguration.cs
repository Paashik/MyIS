using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;
using MyIS.Core.Domain.Users;

namespace MyIS.Core.Infrastructure.Data.Configurations.Requests;

public class RequestAttachmentConfiguration : IEntityTypeConfiguration<RequestAttachment>
{
    public void Configure(EntityTypeBuilder<RequestAttachment> builder)
    {
        builder.ToTable("request_attachments", "requests");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedOnAdd();

        builder.Property(a => a.RequestId)
            .HasColumnName("request_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => new RequestId(value));

        builder.Property(a => a.FileName)
            .HasColumnName("file_name")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(a => a.FilePath)
            .HasColumnName("file_path")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(a => a.UploadedBy)
            .HasColumnName("uploaded_by")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(a => a.UploadedAt)
            .HasColumnName("uploaded_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(a => a.Request)
            .WithMany(r => r.Attachments)
            .HasForeignKey(a => a.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK на core.users.uploaded_by
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.UploadedBy)
            .HasPrincipalKey(u => u.Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}