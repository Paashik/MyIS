using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyIS.Core.Domain.Customers.Entities;
using MyIS.Core.Domain.Mdm.Entities;
using MyIS.Core.Domain.Organization;

namespace MyIS.Core.Infrastructure.Data.Configurations;

public class CustomerOrderConfiguration : IEntityTypeConfiguration<CustomerOrder>
{
    public void Configure(EntityTypeBuilder<CustomerOrder> builder)
    {
        builder.ToTable("customer_orders", "customers");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(o => o.Number)
            .HasColumnName("number")
            .HasMaxLength(50)
            .HasColumnType("character varying(50)");

        builder.Property(o => o.OrderDate)
            .HasColumnName("order_date")
            .HasColumnType("timestamp without time zone");

        builder.Property(o => o.DeliveryDate)
            .HasColumnName("delivery_date")
            .HasColumnType("timestamp without time zone");

        builder.Property(o => o.State)
            .HasColumnName("state")
            .HasColumnType("integer");

        builder.Property(o => o.CustomerId)
            .HasColumnName("customer_id")
            .HasColumnType("uuid");

        builder.Property(o => o.PersonId)
            .HasColumnName("person_id")
            .HasColumnType("uuid");

        builder.Property(o => o.Note)
            .HasColumnName("note")
            .HasMaxLength(100)
            .HasColumnType("character varying(100)");

        builder.Property(o => o.Contract)
            .HasColumnName("contract")
            .HasMaxLength(30)
            .HasColumnType("character varying(30)");

        builder.Property(o => o.StoreId)
            .HasColumnName("store_id")
            .HasColumnType("integer");

        builder.Property(o => o.Path)
            .HasColumnName("path")
            .HasMaxLength(255)
            .HasColumnType("character varying(255)");

        builder.Property(o => o.PayDate)
            .HasColumnName("pay_date")
            .HasColumnType("timestamp without time zone");

        builder.Property(o => o.FinishedDate)
            .HasColumnName("finished_date")
            .HasColumnType("timestamp without time zone");

        builder.Property(o => o.ContactId)
            .HasColumnName("contact_id")
            .HasColumnType("integer");

        builder.Property(o => o.Discount)
            .HasColumnName("discount")
            .HasColumnType("integer");

        builder.Property(o => o.Tax)
            .HasColumnName("tax")
            .HasColumnType("integer");

        builder.Property(o => o.Mark)
            .HasColumnName("mark")
            .HasColumnType("integer");

        builder.Property(o => o.Pn)
            .HasColumnName("pn")
            .HasColumnType("integer");

        builder.Property(o => o.PaymentForm)
            .HasColumnName("payment_form")
            .HasColumnType("integer");

        builder.Property(o => o.PayMethod)
            .HasColumnName("pay_method")
            .HasColumnType("integer");

        builder.Property(o => o.PayPeriod)
            .HasColumnName("pay_period")
            .HasColumnType("integer");

        builder.Property(o => o.Prepayment)
            .HasColumnName("prepayment")
            .HasColumnType("integer");

        builder.Property(o => o.Kind)
            .HasColumnName("kind")
            .HasColumnType("integer");

        builder.Property(o => o.AccountId)
            .HasColumnName("account_id")
            .HasColumnType("integer");

        builder.Property(o => o.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(o => o.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<Counterparty>()
            .WithMany()
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(o => o.PersonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
