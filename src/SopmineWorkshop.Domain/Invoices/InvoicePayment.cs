using SopmineWorkshop.Domain.Common;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Enums;

namespace SopmineWorkshop.Domain.Invoices;

public sealed class InvoicePayment : AuditableEntity
{
    public Guid InvoiceId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime PaymentDate { get; private set; }

    // Nullable only to retain migration support for protected opening-balance rows.
    public InvoicePaymentMethod? Method { get; private set; }

    public string? Reference { get; private set; }
    public string? Note { get; private set; }
    public bool IsOpeningBalance { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public string? CancellationReason { get; private set; }
    public Invoice? Invoice { get; set; }

    private InvoicePayment()
    {
    }

    private InvoicePayment(
        Guid id,
        Guid invoiceId,
        decimal amount,
        DateTime paymentDate,
        InvoicePaymentMethod? method,
        string? reference,
        string? note,
        bool isOpeningBalance)
        : base(id)
    {
        if (!isOpeningBalance && !method.HasValue)
            throw new ArgumentException("Only an opening-balance payment may omit its payment method.", nameof(method));

        InvoiceId = invoiceId;
        Amount = amount;
        PaymentDate = paymentDate;
        Method = method;
        Reference = reference;
        Note = note;
        IsOpeningBalance = isOpeningBalance;
    }

    public static Result<InvoicePayment> Create(
        Guid id,
        Guid invoiceId,
        decimal amount,
        DateTime paymentDate,
        InvoicePaymentMethod method,
        string? reference,
        string? note)
    {
        if (invoiceId == Guid.Empty)
            return InvoicePaymentErrors.InvoiceIdRequired;

        if (!Enum.IsDefined(method))
            return InvoicePaymentErrors.MethodInvalid;

        var roundedAmount = RoundCurrency(amount);
        if (roundedAmount <= 0)
            return InvoicePaymentErrors.AmountInvalid;

        return new InvoicePayment(
            id,
            invoiceId,
            roundedAmount,
            paymentDate,
            method,
            NormalizeOptional(reference, 100),
            NormalizeOptional(note, 500),
            isOpeningBalance: false);
    }

    internal void AssignToInvoice(Guid invoiceId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(invoiceId, Guid.Empty);
        InvoiceId = invoiceId;
    }

    public Result<Updated> Cancel(DateTimeOffset cancelledAtUtc, string? reason)
    {
        if (CancelledAtUtc.HasValue)
            return InvoicePaymentErrors.AlreadyCancelled;

        CancelledAtUtc = cancelledAtUtc;
        CancellationReason = NormalizeOptional(reason, 500);

        return Result.Updated;
    }

    private static decimal RoundCurrency(decimal amount)
        => Math.Round(amount, 2, MidpointRounding.AwayFromZero);

    private static string? NormalizeOptional(string? value, int maximumLength)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return null;

        return trimmed.Length <= maximumLength ? trimmed : trimmed[..maximumLength];
    }
}
