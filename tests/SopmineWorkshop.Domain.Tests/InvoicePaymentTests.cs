using SopmineWorkshop.Domain.Enums;
using SopmineWorkshop.Domain.Invoices;
using Xunit;

namespace SopmineWorkshop.Domain.Tests;

public sealed class InvoicePaymentTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    public void Create_rejects_zero_or_negative_amounts(decimal amount)
    {
        var result = InvoicePayment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            amount,
            new DateTime(2026, 7, 22),
            InvoicePaymentMethod.Virement,
            null,
            null);

        Assert.True(result.IsError);
        Assert.Equal(InvoicePaymentErrors.AmountInvalid.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_normalizes_and_caps_payment_text()
    {
        var payment = InvoicePayment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            10m,
            new DateTime(2026, 7, 22),
            InvoicePaymentMethod.Virement,
            $"  {new string('r', 101)}  ",
            $"  {new string('n', 501)}  ").Value;

        payment.Cancel(new DateTimeOffset(2026, 7, 23, 0, 0, 0, TimeSpan.Zero), $"  {new string('c', 501)}  ");

        Assert.Equal(100, payment.Reference!.Length);
        Assert.Equal(500, payment.Note!.Length);
        Assert.Equal(500, payment.CancellationReason!.Length);
    }

    [Theory]
    [InlineData(InvoiceStatus.Draft)]
    [InlineData(InvoiceStatus.Cancelled)]
    public void RecordPayment_rejects_draft_or_cancelled_invoices(InvoiceStatus status)
    {
        var invoice = CreateInvoice(status: status);

        var result = invoice.RecordPayment(
            Guid.NewGuid(),
            10m,
            new DateTime(2026, 7, 22),
            InvoicePaymentMethod.Virement,
            null,
            null);

        Assert.True(result.IsError);
        Assert.Equal(InvoiceErrors.PaymentInvoiceNotPayable.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData(InvoiceType.Devis)]
    public void RecordPayment_rejects_non_facture_invoice_types(InvoiceType type)
    {
        var invoice = CreateInvoice(type: type);

        var result = invoice.RecordPayment(
            Guid.NewGuid(),
            10m,
            new DateTime(2026, 7, 22),
            InvoicePaymentMethod.Virement,
            null,
            null);

        Assert.True(result.IsError);
        Assert.Equal(InvoiceErrors.PaymentInvoiceTypeNotSupported.Code, result.TopError.Code);
    }

    [Fact]
    public void RecordPayment_accepts_sale_delivery_invoice_before_conversion()
    {
        var invoice = CreateInvoice(type: InvoiceType.BonLivraison);

        var result = invoice.RecordPayment(
            Guid.NewGuid(),
            10m,
            new DateTime(2026, 7, 22),
            InvoicePaymentMethod.Virement,
            null,
            null);

        Assert.False(result.IsError);
        Assert.Single(invoice.Payments);
    }
    [Fact]
    public void RecordPayment_tracks_partial_installments_and_uses_rounded_currency()
    {
        var invoice = CreateInvoice(total: 100m);

        var first = invoice.RecordPayment(Guid.NewGuid(), 33.335m, new DateTime(2026, 7, 22), InvoicePaymentMethod.Espece, " ref ", " note ");
        var second = invoice.RecordPayment(Guid.NewGuid(), 16.665m, new DateTime(2026, 7, 23), InvoicePaymentMethod.Virement, null, null);
        var summary = invoice.GetPaymentSummary(new DateTime(2026, 7, 24));

        Assert.False(first.IsError);
        Assert.False(second.IsError);
        Assert.Equal(33.34m, first.Value.Amount);
        Assert.Equal("ref", first.Value.Reference);
        Assert.Equal("note", first.Value.Note);
        Assert.Equal(50.01m, summary.TotalPaid);
        Assert.Equal(49.99m, summary.RemainingAmount);
        Assert.Equal(InvoicePaymentProgress.PartiallyPaid, summary.Progress);
        Assert.Equal(2, invoice.PaymentRevision);
    }

    [Fact]
    public void RecordPayment_full_settlement_synchronizes_legacy_fields()
    {
        var invoice = CreateInvoice(total: 100m);

        var result = invoice.RecordPayment(Guid.NewGuid(), 100m, new DateTime(2026, 7, 22), InvoicePaymentMethod.Carte, null, null);
        var summary = invoice.GetPaymentSummary(new DateTime(2026, 7, 22));

        Assert.False(result.IsError);
        Assert.Equal(0m, summary.RemainingAmount);
        Assert.Equal(InvoicePaymentProgress.Paid, summary.Progress);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.Equal(InvoicePaymentStatus.Payee, invoice.PaymentStatus);
        Assert.Equal(InvoicePaymentMethod.Carte, invoice.PaymentMethod);
    }

    [Fact]
    public void RecordPayment_rejects_overpayment()
    {
        var invoice = CreateInvoice(total: 100m);

        var result = invoice.RecordPayment(Guid.NewGuid(), 100.01m, new DateTime(2026, 7, 22), InvoicePaymentMethod.Carte, null, null);

        Assert.True(result.IsError);
        Assert.Equal(InvoiceErrors.PaymentExceedsRemainingAmount.Code, result.TopError.Code);
        Assert.Empty(invoice.Payments);
        Assert.Equal(0, invoice.PaymentRevision);
    }

    [Fact]
    public void CancelPayment_reverses_a_settlement_and_cannot_be_repeated()
    {
        var invoice = CreateInvoice(total: 100m);
        var paymentId = Guid.NewGuid();
        invoice.RecordPayment(paymentId, 100m, new DateTime(2026, 7, 22), InvoicePaymentMethod.Cheque, null, null);

        var cancellation = invoice.CancelPayment(paymentId, new DateTimeOffset(2026, 7, 23, 8, 30, 0, TimeSpan.Zero), " duplicate ");
        var repeatCancellation = invoice.CancelPayment(paymentId, new DateTimeOffset(2026, 7, 24, 8, 30, 0, TimeSpan.Zero), null);
        var summary = invoice.GetPaymentSummary(new DateTime(2026, 7, 24));

        Assert.False(cancellation.IsError);
        Assert.True(repeatCancellation.IsError);
        Assert.Equal(InvoicePaymentErrors.AlreadyCancelled.Code, repeatCancellation.TopError.Code);
        Assert.Equal("duplicate", invoice.Payments.Single().CancellationReason);
        Assert.Equal(0m, summary.TotalPaid);
        Assert.Equal(100m, summary.RemainingAmount);
        Assert.Equal(InvoicePaymentProgress.Unpaid, summary.Progress);
        Assert.Equal(InvoiceStatus.Validated, invoice.Status);
        Assert.Equal(InvoicePaymentStatus.NonPayee, invoice.PaymentStatus);
        Assert.Null(invoice.PaymentMethod);
        Assert.Equal(2, invoice.PaymentRevision);
    }

    [Fact]
    public void GetPaymentSummary_marks_outstanding_past_due_invoices_overdue()
    {
        var invoice = CreateInvoice(total: 100m, dueDate: new DateTime(2026, 7, 20));

        var summary = invoice.GetPaymentSummary(new DateTime(2026, 7, 21, 23, 59, 59));

        Assert.Equal(InvoicePaymentProgress.Overdue, summary.Progress);
    }

    [Fact]
    public void Update_rejects_manual_paid_transition_without_active_payment_settlement()
    {
        var invoice = CreateInvoice(total: 100m);

        var result = invoice.Update(
            invoice.Reference,
            invoice.Type,
            invoice.Nature,
            invoice.Date,
            invoice.FournisseurId,
            invoice.ClientId,
            invoice.Total,
            status: InvoiceStatus.Paid,
            dueDate: invoice.DueDate);

        Assert.True(result.IsError);
        Assert.Equal(InvoiceErrors.PaidStatusRequiresSettlement.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_rejects_non_zero_invoice_in_paid_status_without_settlement()
    {
        var invoiceId = Guid.NewGuid();
        var result = Invoice.Create(
            invoiceId,
            "FA-001",
            InvoiceType.Facture,
            InvoiceNature.Vente,
            new DateTime(2026, 7, 1),
            null,
            Guid.NewGuid(),
            100m,
            [CreateLine(invoiceId, 100m)],
            status: InvoiceStatus.Paid);

        Assert.True(result.IsError);
        Assert.Equal(InvoiceErrors.PaidStatusRequiresSettlement.Code, result.TopError.Code);
    }

    [Fact]
    public void UpsertLines_rejects_candidate_total_lower_than_active_paid_amount_before_mutating()
    {
        var invoice = CreateInvoice(total: 100m);
        invoice.RecordPayment(Guid.NewGuid(), 100m, new DateTime(2026, 7, 22), InvoicePaymentMethod.Carte, null, null);

        var result = invoice.UpsertLines([CreateLine(invoice.Id, 50m)]);

        Assert.True(result.IsError);
        Assert.Equal(InvoiceErrors.TotalBelowActivePayments.Code, result.TopError.Code);
        Assert.Equal(100m, invoice.Total);
        Assert.Equal(100m, invoice.Lines.Single().LineTotal);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
    }

    [Fact]
    public void UpsertLines_reopens_paid_invoice_to_validated_when_candidate_total_exceeds_active_payments()
    {
        var invoice = CreateInvoice(total: 100m);
        invoice.RecordPayment(Guid.NewGuid(), 100m, new DateTime(2026, 7, 22), InvoicePaymentMethod.Carte, null, null);

        var result = invoice.UpsertLines([CreateLine(invoice.Id, 150m)]);

        Assert.False(result.IsError);
        Assert.Equal(150m, invoice.Total);
        Assert.Equal(InvoiceStatus.Validated, invoice.Status);
        Assert.Equal(InvoicePaymentStatus.NonPayee, invoice.PaymentStatus);
        Assert.Null(invoice.PaymentMethod);
        Assert.Equal(50m, invoice.GetPaymentSummary(new DateTime(2026, 7, 23)).RemainingAmount);
        Assert.Equal(InvoicePaymentProgress.PartiallyPaid, invoice.GetPaymentSummary(new DateTime(2026, 7, 23)).Progress);
    }

    private static Invoice CreateInvoice(
        decimal total = 100m,
        InvoiceStatus status = InvoiceStatus.Validated,
        InvoiceType type = InvoiceType.Facture,
        DateTime? dueDate = null)
    {
        var invoiceId = Guid.NewGuid();
        var line = CreateLine(invoiceId, total);

        var result = Invoice.Create(
            invoiceId,
            "FA-001",
            type,
            InvoiceNature.Vente,
            new DateTime(2026, 7, 1),
            null,
            Guid.NewGuid(),
            total,
            [line],
            status: status,
            dueDate: dueDate);

        Assert.False(result.IsError);
        return result.Value;
    }

    private static InvoiceLine CreateLine(Guid invoiceId, decimal total)
        => InvoiceLine.Create(
            Guid.NewGuid(),
            invoiceId,
            null,
            "P-001",
            "Product",
            string.Empty,
            "unit",
            1m,
            total,
            0m).Value;
}
