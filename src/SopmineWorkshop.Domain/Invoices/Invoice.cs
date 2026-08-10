using SopmineWorkshop.Domain.Common;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Enums;

namespace SopmineWorkshop.Domain.Invoices;

public sealed class Invoice : AuditableEntity
{
    public string Reference { get; private set; } = string.Empty;
    public InvoiceType Type { get; private set; }
    public InvoiceNature Nature { get; private set; }
    public DateTime Date { get; private set; }
    public DateTime? DueDate { get; private set; }
    public Guid? FournisseurId { get; private set; }
    public Guid? ClientId { get; private set; }
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Draft;
    public InvoicePaymentStatus? PaymentStatus { get; private set; }
    public InvoicePaymentMethod? PaymentMethod { get; private set; }
    public Guid? ConvertedToInvoiceId { get; private set; }
    public string? Notes { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal Total { get; private set; }
    public ICollection<InvoiceLine> Lines { get; private set; } = [];
    public ICollection<InvoicePayment> Payments { get; private set; } = [];
    public long PaymentRevision { get; private set; }

    private Invoice()
    {
    }

    private Invoice(
        Guid id,
        string reference,
        InvoiceType type,
        InvoiceNature nature,
        DateTime date,
        DateTime? dueDate,
        Guid? fournisseurId,
        Guid? clientId,
        InvoiceStatus status,
        InvoicePaymentStatus? paymentStatus,
        InvoicePaymentMethod? paymentMethod,
        Guid? convertedToInvoiceId,
        string? notes,
        decimal subtotal,
        decimal taxTotal,
        decimal total,
        List<InvoiceLine> lines)
        : base(id)
    {
        Reference = reference;
        Type = type;
        Nature = nature;
        Date = date;
        DueDate = dueDate;
        FournisseurId = fournisseurId;
        ClientId = clientId;
        Status = status;
        PaymentStatus = paymentStatus;
        PaymentMethod = paymentMethod;
        ConvertedToInvoiceId = convertedToInvoiceId;
        Notes = notes;
        Subtotal = subtotal;
        TaxTotal = taxTotal;
        Total = total;
        Lines = lines;
    }

    public static Result<Invoice> Create(
        Guid id,
        string reference,
        InvoiceType type,
        InvoiceNature nature,
        DateTime date,
        Guid? fournisseurId,
        Guid? clientId,
        decimal total,
        List<InvoiceLine>? lines = null,
        InvoiceStatus status = InvoiceStatus.Draft,
        InvoicePaymentStatus? paymentStatus = null,
        InvoicePaymentMethod? paymentMethod = null,
        Guid? convertedToInvoiceId = null,
        DateTime? dueDate = null,
        string? notes = null)
    {
        var normalizedReference = reference?.Trim() ?? string.Empty;
        var normalizedNotes = NormalizeOptional(notes);

        if (normalizedReference.Length > 100)
            return InvoiceErrors.ReferenceTooLong;

        if (!Enum.IsDefined(type))
            return InvoiceErrors.TypeInvalid;

        if (!Enum.IsDefined(nature))
            return InvoiceErrors.NatureInvalid;

        if (!IsTypeAllowedForNature(nature, type))
            return InvoiceErrors.TypeNotAllowedForNature;

        if (!Enum.IsDefined(status))
            return InvoiceErrors.StatusInvalid;

        var paymentError = ValidatePayment(nature, type, paymentStatus, paymentMethod);
        if (paymentError is not null)
            return paymentError;

        if (convertedToInvoiceId.HasValue && convertedToInvoiceId.Value == Guid.Empty)
            return InvoiceErrors.NotFound;

        if (dueDate.HasValue && dueDate.Value.Date < date.Date)
            return InvoiceErrors.DueDateInvalid;

        if (fournisseurId.HasValue && fournisseurId.Value == Guid.Empty)
            return InvoiceErrors.FournisseurIdInvalid;

        if (clientId.HasValue && clientId.Value == Guid.Empty)
            return InvoiceErrors.ClientIdInvalid;

        var counterpartError = ValidateCounterpart(nature, fournisseurId, clientId);
        if (counterpartError is not null)
            return counterpartError;

        if (total < 0)
            return InvoiceErrors.TotalInvalid;

        lines ??= [];

        var (subtotal, taxTotal, computedTotal) = CalculateTotals(lines);

        if (status == InvoiceStatus.Paid && computedTotal > 0)
            return InvoiceErrors.PaidStatusRequiresSettlement;

        return new Invoice(
            id,
            normalizedReference,
            type,
            nature,
            date,
            dueDate,
            fournisseurId,
            clientId,
            status,
            NormalizePaymentStatus(nature, type, paymentStatus),
            NormalizePaymentMethod(paymentStatus, paymentMethod),
            convertedToInvoiceId,
            normalizedNotes,
            subtotal,
            taxTotal,
            computedTotal,
            lines);
    }

    public Result<Updated> Update(
        string reference,
        InvoiceType type,
        InvoiceNature nature,
        DateTime date,
        Guid? fournisseurId,
        Guid? clientId,
        decimal total,
        InvoiceStatus? status = null,
        InvoicePaymentStatus? paymentStatus = null,
        InvoicePaymentMethod? paymentMethod = null,
        DateTime? dueDate = null,
        string? notes = null)
    {
        var normalizedReference = reference?.Trim() ?? string.Empty;
        var normalizedNotes = NormalizeOptional(notes);

        if (normalizedReference.Length > 100)
            return InvoiceErrors.ReferenceTooLong;

        if (!Enum.IsDefined(type))
            return InvoiceErrors.TypeInvalid;

        if (!Enum.IsDefined(nature))
            return InvoiceErrors.NatureInvalid;

        if (!IsTypeAllowedForNature(nature, type))
            return InvoiceErrors.TypeNotAllowedForNature;

        if (status.HasValue && !Enum.IsDefined(status.Value))
            return InvoiceErrors.StatusInvalid;

        var paymentError = ValidatePayment(nature, type, paymentStatus, paymentMethod);
        if (paymentError is not null)
            return paymentError;

        if (dueDate.HasValue && dueDate.Value.Date < date.Date)
            return InvoiceErrors.DueDateInvalid;

        if (fournisseurId.HasValue && fournisseurId.Value == Guid.Empty)
            return InvoiceErrors.FournisseurIdInvalid;

        if (clientId.HasValue && clientId.Value == Guid.Empty)
            return InvoiceErrors.ClientIdInvalid;

        var counterpartError = ValidateCounterpart(nature, fournisseurId, clientId);
        if (counterpartError is not null)
            return counterpartError;

        if (total < 0)
            return InvoiceErrors.TotalInvalid;

        if (status == InvoiceStatus.Paid && GetActivePaidAmount() < Total)
            return InvoiceErrors.PaidStatusRequiresSettlement;

        Reference = normalizedReference;
        Type = type;
        Nature = nature;
        Date = date;
        DueDate = dueDate;
        FournisseurId = fournisseurId;
        ClientId = clientId;
        if (GetActivePaidAmount() > 0)
        {
            // Active payments are authoritative: editing invoice metadata must not
            // overwrite the settlement-derived status or payment method.
            SynchronizeLegacyPaymentFields();
        }
        else
        {
            Status = status ?? Status;
            PaymentStatus = NormalizePaymentStatus(nature, type, paymentStatus);
            PaymentMethod = NormalizePaymentMethod(paymentStatus, paymentMethod);
        }

        Notes = normalizedNotes;

        return Result.Updated;
    }

    public Result<Updated> AssignReference(string? reference)
    {
        var normalizedReference = reference?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedReference))
            return InvoiceErrors.ReferenceRequired;

        if (normalizedReference.Length > 100)
            return InvoiceErrors.ReferenceTooLong;

        Reference = normalizedReference;
        return Result.Updated;
    }

    public Result<InvoicePayment> RecordPayment(
        Guid paymentId,
        decimal amount,
        DateTime paymentDate,
        InvoicePaymentMethod method,
        string? reference,
        string? note)
    {
        if (ConvertedToInvoiceId.HasValue)
            return InvoiceErrors.ConvertedSourceLocked;

        if (!RequiresPayment(Nature, Type))
            return InvoiceErrors.PaymentInvoiceTypeNotSupported;

        if (Status is InvoiceStatus.Draft or InvoiceStatus.Cancelled)
            return InvoiceErrors.PaymentInvoiceNotPayable;

        var paymentResult = InvoicePayment.Create(paymentId, Id, amount, paymentDate, method, reference, note);
        if (paymentResult.IsError)
            return paymentResult.Errors;

        var payment = paymentResult.Value;
        if (payment.Amount > GetPaymentSummary(paymentDate).RemainingAmount)
            return InvoiceErrors.PaymentExceedsRemainingAmount;

        Payments.Add(payment);
        PaymentRevision++;
        SynchronizeLegacyPaymentFields();

        return payment;
    }

    public Result<Updated> CancelPayment(Guid paymentId, DateTimeOffset cancelledAtUtc, string? reason)
    {
        var payment = Payments.FirstOrDefault(candidate => candidate.Id == paymentId);
        if (payment is null)
            return InvoiceErrors.PaymentNotFound;

        var cancellationResult = payment.Cancel(cancelledAtUtc, reason);
        if (cancellationResult.IsError)
            return cancellationResult.Errors;

        PaymentRevision++;
        SynchronizeLegacyPaymentFields();

        return Result.Updated;
    }

    public void TransferPaymentsTo(Invoice target)
    {
        ArgumentNullException.ThrowIfNull(target);

        var payments = Payments.ToList();
        foreach (var payment in payments)
        {
            Payments.Remove(payment);
            payment.AssignToInvoice(target.Id);
            target.Payments.Add(payment);
        }

        if (payments.Count > 0)
        {
            PaymentRevision++;
            target.PaymentRevision++;
        }

        SynchronizeLegacyPaymentFields();
        target.SynchronizeLegacyPaymentFields();
    }

    public InvoicePaymentSummary GetPaymentSummary(DateTime asOf)
    {
        var totalPaid = GetActivePaidAmount();
        var remainingAmount = RoundCurrency(Total - totalPaid);
        var progress = totalPaid > 0 && remainingAmount <= 0
            ? InvoicePaymentProgress.Paid
            : DueDate.HasValue && DueDate.Value.Date < asOf.Date
                ? InvoicePaymentProgress.Overdue
                : totalPaid > 0
                    ? InvoicePaymentProgress.PartiallyPaid
                    : InvoicePaymentProgress.Unpaid;

        return new InvoicePaymentSummary(totalPaid, remainingAmount, progress);
    }

    private void SynchronizeLegacyPaymentFields()
    {
        var totalPaid = GetActivePaidAmount();
        var remainingAmount = RoundCurrency(Total - totalPaid);
        if (totalPaid > 0 && remainingAmount <= 0)
        {
            Status = InvoiceStatus.Paid;
            PaymentStatus = InvoicePaymentStatus.Payee;
            PaymentMethod = Payments
                .Where(payment => payment.CancelledAtUtc is null && !payment.IsOpeningBalance)
                .OrderBy(payment => payment.PaymentDate)
                .LastOrDefault()
                ?.Method;
            return;
        }

        Status = InvoiceStatus.Validated;
        PaymentStatus = InvoicePaymentStatus.NonPayee;
        PaymentMethod = null;
    }

    private decimal GetActivePaidAmount()
        => RoundCurrency(Payments.Where(payment => payment.CancelledAtUtc is null).Sum(payment => payment.Amount));

    private static decimal RoundCurrency(decimal amount)
        => Math.Round(amount, 2, MidpointRounding.AwayFromZero);

    public Result<Updated> MarkConvertedTo(Guid invoiceId)
    {
        if (invoiceId == Guid.Empty)
            return InvoiceErrors.NotFound;

        if (ConvertedToInvoiceId.HasValue)
            return InvoiceErrors.AlreadyConverted;

        ConvertedToInvoiceId = invoiceId;
        PaymentStatus = null;
        PaymentMethod = null;
        return Result.Updated;
    }

    public void ClearConvertedTo()
    {
        ConvertedToInvoiceId = null;
        SynchronizeLegacyPaymentFields();
    }

    private static bool IsTypeAllowedForNature(InvoiceNature nature, InvoiceType type)
    {
        return nature switch
        {
            InvoiceNature.Achat => type is InvoiceType.BonCommande
                or InvoiceType.BonReception
                or InvoiceType.Facture
                or InvoiceType.Avoir,
            InvoiceNature.Vente => type is InvoiceType.Devis
                or InvoiceType.BonCommande
                or InvoiceType.BonLivraison
                or InvoiceType.Facture
                or InvoiceType.Avoir,
            _ => false
        };
    }

    private static Error? ValidateCounterpart(InvoiceNature nature, Guid? fournisseurId, Guid? clientId)
    {
        return nature switch
        {
            InvoiceNature.Achat when clientId.HasValue => InvoiceErrors.ClientForbiddenForAchat,
            InvoiceNature.Vente when fournisseurId.HasValue => InvoiceErrors.FournisseurForbiddenForVente,
            _ => null
        };
    }

    public static bool RequiresPayment(InvoiceNature nature, InvoiceType type)
    {
        return (nature, type) is
            (InvoiceNature.Achat, InvoiceType.Facture) or
            (InvoiceNature.Vente, InvoiceType.BonLivraison) or
            (InvoiceNature.Vente, InvoiceType.Facture);
    }

    private static Error? ValidatePayment(
        InvoiceNature nature,
        InvoiceType type,
        InvoicePaymentStatus? paymentStatus,
        InvoicePaymentMethod? paymentMethod)
    {
        if (!RequiresPayment(nature, type))
            return null;

        if (!paymentStatus.HasValue)
            return null;

        if (!Enum.IsDefined(paymentStatus.Value))
            return InvoiceErrors.PaymentStatusInvalid;

        if (paymentStatus.Value == InvoicePaymentStatus.Payee)
        {
            if (paymentMethod.HasValue && !Enum.IsDefined(paymentMethod.Value))
                return InvoiceErrors.PaymentMethodInvalid;
        }

        return null;
    }

    private static InvoicePaymentStatus? NormalizePaymentStatus(
        InvoiceNature nature,
        InvoiceType type,
        InvoicePaymentStatus? paymentStatus)
    {
        return RequiresPayment(nature, type) ? paymentStatus : null;
    }

    private static InvoicePaymentMethod? NormalizePaymentMethod(
        InvoicePaymentStatus? paymentStatus,
        InvoicePaymentMethod? paymentMethod)
    {
        return paymentStatus == InvoicePaymentStatus.Payee ? paymentMethod : null;
    }

    public Result<Updated> UpsertLines(List<InvoiceLine> incomingLines)
    {
        incomingLines ??= [];

        var (_, _, candidateTotal) = CalculateTotals(incomingLines);
        if (candidateTotal < GetActivePaidAmount())
            return InvoiceErrors.TotalBelowActivePayments;

        foreach (var existingLine in Lines.ToList())
        {
            if (incomingLines.All(line => line.Id != existingLine.Id))
            {
                Lines.Remove(existingLine);
            }
        }

        foreach (var incomingLine in incomingLines)
        {
            var existingLine = Lines.FirstOrDefault(line => line.Id == incomingLine.Id);

            if (existingLine is null)
            {
                Lines.Add(incomingLine);
            }
            else
            {
                var updateResult = existingLine.Update(
                    incomingLine.ProduitId,
                    incomingLine.ProductReference,
                    incomingLine.ProductName,
                    incomingLine.ProductFamily,
                    incomingLine.ProductUnit,
                    incomingLine.Quantity,
                    incomingLine.Price,
                    incomingLine.TVA,
                    incomingLine.LineSubtotal,
                    incomingLine.LineTax,
                    incomingLine.LineTotal,
                    incomingLine.LineOrder);

                if (updateResult.IsError)
                    return updateResult.Errors;
            }
        }

        RecalculateTotals();

        if (GetActivePaidAmount() > 0)
            SynchronizeLegacyPaymentFields();

        return Result.Updated;
    }

    private void RecalculateTotals()
    {
        var (subtotal, taxTotal, total) = CalculateTotals(Lines);

        Subtotal = subtotal;
        TaxTotal = taxTotal;
        Total = total;
    }

    private static (decimal Subtotal, decimal TaxTotal, decimal Total) CalculateTotals(IEnumerable<InvoiceLine> lines)
    {
        var subtotal = Math.Round(lines.Sum(line => line.LineSubtotal), 2, MidpointRounding.AwayFromZero);
        var taxTotal = Math.Round(lines.Sum(line => line.LineTax), 2, MidpointRounding.AwayFromZero);
        var total = subtotal + taxTotal;

        return (subtotal, taxTotal, total);
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
