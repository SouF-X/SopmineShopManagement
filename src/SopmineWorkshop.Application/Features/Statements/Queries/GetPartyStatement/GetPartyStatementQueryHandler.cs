using MediatR;
using Microsoft.EntityFrameworkCore;
using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Application.Features.Statements.Dtos;
using SopmineWorkshop.Domain.Clients;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Enums;
using SopmineWorkshop.Domain.Invoices;
using SopmineWorkshop.Domain.Fournisseurs;

namespace SopmineWorkshop.Application.Features.Statements.Queries.GetPartyStatement;

public sealed class GetPartyStatementQueryHandler(IAppDbContext context) : IRequestHandler<GetPartyStatementQuery, Result<PartyStatementDto>>
{
    public async Task<Result<PartyStatementDto>> Handle(GetPartyStatementQuery query, CancellationToken ct)
    {
        var partyName = query.PartyKind == StatementPartyKind.Client
            ? await context.Clients.Where(x => x.Id == query.PartyId).Select(x => x.Nom).FirstOrDefaultAsync(ct)
            : await context.Fournisseurs.Where(x => x.Id == query.PartyId).Select(x => x.Nom).FirstOrDefaultAsync(ct);
        if (partyName is null)
            return query.PartyKind == StatementPartyKind.Client ? ClientErrors.NotFound : FournisseurErrors.NotFound;

        var documents = await context.Invoices.AsNoTracking().Include(x => x.Payments)
            .Where(x => (x.Status == InvoiceStatus.Validated || x.Status == InvoiceStatus.Paid) &&
                (query.PartyKind == StatementPartyKind.Client
                    ? x.Nature == InvoiceNature.Vente && x.ClientId == query.PartyId &&
                      (x.Type == InvoiceType.BonLivraison || x.Type == InvoiceType.Facture || x.Type == InvoiceType.Avoir)
                    : x.Nature == InvoiceNature.Achat && x.FournisseurId == query.PartyId &&
                      (x.Type == InvoiceType.BonReception || x.Type == InvoiceType.Facture || x.Type == InvoiceType.Avoir)))
            .ToListAsync(ct);

        var asOf = (query.To ?? DateTime.Today).Date;
        var from = query.From?.Date;
        var to = query.To?.Date;
        var statusDocuments = query.PaymentProgress.HasValue
            ? documents.Where(document => IsPaymentTracked(document) && MatchesPaymentProgress(document.GetPaymentSummary(asOf).Progress, query.PaymentProgress.Value)).ToList()
            : documents;
        var statusDocumentIds = statusDocuments.Select(document => document.Id).ToHashSet();

        // A lower bound turns movements before the selected period into the opening balance.
        // The opening balance always uses the complete account, even when rows are filtered by payment status.
        var opening = !from.HasValue
            ? 0m
            : documents.Where(document => document.Date.Date < from.Value).Sum(DocumentBalanceImpact)
              - documents.SelectMany(document => document.Payments.Select(payment => new { document, payment }))
                  .Where(item => item.payment.CancelledAtUtc is null && item.payment.PaymentDate.Date < from.Value && PaymentAffectsBalance(item.document))
                  .Sum(item => item.payment.Amount);

        var rows = new List<(DateTime Date, bool IsPayment, DateTimeOffset Created, Guid Id, StatementMovementDto Item)>();
        foreach (var document in documents)
        {
            var documentImpact = DocumentBalanceImpact(document);
            var paymentProgress = IsPaymentTracked(document) ? (InvoicePaymentProgress?)document.GetPaymentSummary(asOf).Progress : null;

            if (IsWithinPeriod(document.Date, from, to))
                rows.Add((document.Date, false, document.CreatedAtUtc, document.Id, new StatementMovementDto
                {
                    MovementId = document.Id,
                    InvoiceId = document.Id,
                    MovementDate = document.Date,
                    CreatedAtUtc = document.CreatedAtUtc,
                    Reference = document.Reference,
                    DocumentType = document.Type,
                    MovementType = DocumentMovementType(document),
                    DocumentAmount = Math.Abs(document.Total),
                    BalanceImpact = documentImpact,
                    PaymentProgress = paymentProgress,
                    IsInformational = documentImpact == 0,
                    InvoicedAmount = documentImpact,
                }));

            foreach (var payment in document.Payments.Where(payment => IsWithinPeriod(payment.PaymentDate, from, to)))
            {
                var active = payment.CancelledAtUtc is null;
                var affectsBalance = PaymentAffectsBalance(document);
                var paidAmount = active && affectsBalance ? payment.Amount : 0m;
                rows.Add((payment.PaymentDate, true, payment.CreatedAtUtc, payment.Id, new StatementMovementDto
                {
                    MovementId = payment.Id,
                    InvoiceId = document.Id,
                    PaymentId = payment.Id,
                    MovementDate = payment.PaymentDate,
                    CreatedAtUtc = payment.CreatedAtUtc,
                    Reference = payment.Reference ?? document.Reference,
                    Method = payment.Method,
                    DocumentType = document.Type,
                    MovementType = active ? "R\u00e8glement" : "R\u00e8glement annul\u00e9",
                    DocumentAmount = payment.Amount,
                    BalanceImpact = -paidAmount,
                    PaidAmount = paidAmount,
                    IsInformational = !affectsBalance,
                    IsCancelled = !active,
                }));
            }
        }

        var balance = RoundCurrency(opening);
        var accountMovements = new List<StatementMovementDto>();
        foreach (var row in rows.OrderBy(x => x.Date).ThenBy(x => x.IsPayment).ThenBy(x => x.Created).ThenBy(x => x.Id))
        {
            var movement = row.Item;
            balance = RoundCurrency(balance + movement.BalanceImpact);
            accountMovements.Add(new StatementMovementDto
            {
                MovementId = movement.MovementId,
                InvoiceId = movement.InvoiceId,
                PaymentId = movement.PaymentId,
                MovementDate = movement.MovementDate,
                CreatedAtUtc = movement.CreatedAtUtc,
                Reference = movement.Reference,
                Method = movement.Method,
                DocumentType = movement.DocumentType,
                MovementType = movement.MovementType,
                DocumentAmount = movement.DocumentAmount,
                BalanceImpact = movement.BalanceImpact,
                PaymentProgress = movement.PaymentProgress,
                IsInformational = movement.IsInformational,
                InvoicedAmount = movement.InvoicedAmount,
                PaidAmount = movement.PaidAmount,
                IsCancelled = movement.IsCancelled,
                RunningBalance = balance,
            });
        }

        var movements = query.PaymentProgress.HasValue
            ? accountMovements.Where(movement => statusDocumentIds.Contains(movement.InvoiceId)).ToList()
            : accountMovements;

        // A status filter is a scoped view of the matching documents. Rebuild its
        // running balance from zero so hidden account movements cannot make a
        // paid document appear to finish with another document's balance.
        if (query.PaymentProgress.HasValue)
        {
            var filteredBalance = 0m;
            movements = movements.Select(movement =>
            {
                filteredBalance = RoundCurrency(filteredBalance + movement.BalanceImpact);
                return CopyWithRunningBalance(movement, filteredBalance);
            }).ToList();
        }

        var overdue = documents.Where(IsPaymentTracked)
            .Where(document => document.DueDate.HasValue && document.DueDate.Value.Date < asOf)
            .Sum(document => Math.Max(0, document.Total - document.Payments.Where(payment => payment.CancelledAtUtc is null).Sum(payment => payment.Amount)));
        var totalInvoiced = accountMovements.Where(movement => movement.PaymentId is null && movement.InvoicedAmount > 0).Sum(movement => movement.InvoicedAmount);
        var totalCredits = accountMovements.Where(movement => movement.PaymentId is null && movement.InvoicedAmount < 0).Sum(movement => -movement.InvoicedAmount);
        var totalPaid = accountMovements.Where(movement => movement.PaymentId.HasValue).Sum(movement => movement.PaidAmount);

        return new PartyStatementDto
        {
            PartyId = query.PartyId,
            PartyName = partyName,
            From = from,
            To = to,
            OpeningBalance = RoundCurrency(opening),
            TotalInvoiced = RoundCurrency(totalInvoiced),
            TotalCredits = RoundCurrency(totalCredits),
            TotalPaid = RoundCurrency(totalPaid),
            RemainingBalance = balance,
            OverdueAmount = RoundCurrency(overdue),
            Movements = movements,
        };
    }

    private static bool IsPaymentTracked(Invoice document)
        => Invoice.RequiresPayment(document.Nature, document.Type) && !document.ConvertedToInvoiceId.HasValue;

    private static bool PaymentAffectsBalance(Invoice document)
        => IsPaymentTracked(document);

    private static bool MatchesPaymentProgress(InvoicePaymentProgress actual, InvoicePaymentProgress requested)
        => requested == InvoicePaymentProgress.Unpaid
            ? actual != InvoicePaymentProgress.Paid
            : actual == requested;

    private static decimal DocumentBalanceImpact(Invoice document)
    {
        if (document.ConvertedToInvoiceId.HasValue)
            return 0m;

        return document.Type switch
        {
            InvoiceType.Avoir => -document.Total,
            InvoiceType.Facture when Invoice.RequiresPayment(document.Nature, document.Type) => document.Total,
            InvoiceType.BonLivraison when document.Nature == InvoiceNature.Vente => document.Total,
            _ => 0m,
        };
    }

    private static string DocumentMovementType(Invoice document)
        => document.ConvertedToInvoiceId.HasValue && document.Type == InvoiceType.BonLivraison
            ? "Bon de livraison (converti)"
            : (document.Nature, document.Type) switch
            {
                (InvoiceNature.Achat, InvoiceType.BonReception) => "Bon de r\u00e9ception",
                (InvoiceNature.Vente, InvoiceType.BonLivraison) => "Bon de livraison",
                (_, InvoiceType.Facture) => "Facture",
                (_, InvoiceType.Avoir) => "Avoir",
                _ => "Document",
            };

    private static bool IsWithinPeriod(DateTime date, DateTime? from, DateTime? to)
        => (!from.HasValue || date.Date >= from.Value.Date) && (!to.HasValue || date.Date <= to.Value.Date);

    private static decimal RoundCurrency(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static StatementMovementDto CopyWithRunningBalance(StatementMovementDto movement, decimal runningBalance)
        => new()
        {
            MovementId = movement.MovementId,
            InvoiceId = movement.InvoiceId,
            PaymentId = movement.PaymentId,
            MovementDate = movement.MovementDate,
            CreatedAtUtc = movement.CreatedAtUtc,
            Reference = movement.Reference,
            Method = movement.Method,
            DocumentType = movement.DocumentType,
            MovementType = movement.MovementType,
            DocumentAmount = movement.DocumentAmount,
            BalanceImpact = movement.BalanceImpact,
            PaymentProgress = movement.PaymentProgress,
            IsInformational = movement.IsInformational,
            InvoicedAmount = movement.InvoicedAmount,
            PaidAmount = movement.PaidAmount,
            RunningBalance = runningBalance,
            IsCancelled = movement.IsCancelled,
        };
}
