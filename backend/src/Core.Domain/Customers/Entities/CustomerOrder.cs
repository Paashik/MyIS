using System;

namespace MyIS.Core.Domain.Customers.Entities;

public class CustomerOrder
{
    public Guid Id { get; private set; }

    public string? Number { get; private set; }
    public DateTime? OrderDate { get; private set; }
    public DateTime? DeliveryDate { get; private set; }
    public int? State { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid? PersonId { get; private set; }
    public string? Note { get; private set; }
    public string? Contract { get; private set; }
    public int? StoreId { get; private set; }
    public string? Path { get; private set; }
    public DateTime? PayDate { get; private set; }
    public DateTime? FinishedDate { get; private set; }
    public int? ContactId { get; private set; }
    public int? Discount { get; private set; }
    public int? Tax { get; private set; }
    public int? Mark { get; private set; }
    public int? Pn { get; private set; }
    public int? PaymentForm { get; private set; }
    public int? PayMethod { get; private set; }
    public int? PayPeriod { get; private set; }
    public int? Prepayment { get; private set; }
    public int? Kind { get; private set; }
    public int? AccountId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private CustomerOrder()
    {
    }

    public CustomerOrder(
        string? number,
        DateTime? orderDate,
        DateTime? deliveryDate,
        int? state,
        Guid? customerId,
        Guid? personId,
        string? note,
        string? contract,
        int? storeId,
        string? path,
        DateTime? payDate,
        DateTime? finishedDate,
        int? contactId,
        int? discount,
        int? tax,
        int? mark,
        int? pn,
        int? paymentForm,
        int? payMethod,
        int? payPeriod,
        int? prepayment,
        int? kind,
        int? accountId)
    {
        Id = Guid.NewGuid();
        Number = NormalizeOptional(number);
        OrderDate = orderDate;
        DeliveryDate = deliveryDate;
        State = state;
        CustomerId = customerId;
        PersonId = personId;
        Note = NormalizeOptional(note);
        Contract = NormalizeOptional(contract);
        StoreId = storeId;
        Path = NormalizeOptional(path);
        PayDate = payDate;
        FinishedDate = finishedDate;
        ContactId = contactId;
        Discount = discount;
        Tax = tax;
        Mark = mark;
        Pn = pn;
        PaymentForm = paymentForm;
        PayMethod = payMethod;
        PayPeriod = payPeriod;
        Prepayment = prepayment;
        Kind = kind;
        AccountId = accountId;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateFromExternal(
        string? number,
        DateTime? orderDate,
        DateTime? deliveryDate,
        int? state,
        Guid? customerId,
        Guid? personId,
        string? note,
        string? contract,
        int? storeId,
        string? path,
        DateTime? payDate,
        DateTime? finishedDate,
        int? contactId,
        int? discount,
        int? tax,
        int? mark,
        int? pn,
        int? paymentForm,
        int? payMethod,
        int? payPeriod,
        int? prepayment,
        int? kind,
        int? accountId)
    {
        Number = NormalizeOptional(number);
        OrderDate = orderDate;
        DeliveryDate = deliveryDate;
        State = state;
        CustomerId = customerId;
        PersonId = personId;
        Note = NormalizeOptional(note);
        Contract = NormalizeOptional(contract);
        StoreId = storeId;
        Path = NormalizeOptional(path);
        PayDate = payDate;
        FinishedDate = finishedDate;
        ContactId = contactId;
        Discount = discount;
        Tax = tax;
        Mark = mark;
        Pn = pn;
        PaymentForm = paymentForm;
        PayMethod = payMethod;
        PayPeriod = payPeriod;
        Prepayment = prepayment;
        Kind = kind;
        AccountId = accountId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string? NormalizeOptional(string? value)
    {
        value = value?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
