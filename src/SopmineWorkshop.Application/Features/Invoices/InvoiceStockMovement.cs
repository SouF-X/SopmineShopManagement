using Microsoft.EntityFrameworkCore;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Enums;
using SopmineWorkshop.Domain.Invoices;
using SopmineWorkshop.Domain.Produits;

namespace SopmineWorkshop.Application.Features.Invoices;

internal static class InvoiceStockMovement
{
    public static IReadOnlyDictionary<Guid, decimal> Capture(Invoice invoice)
    {
        var direction = GetDirection(invoice.Nature, invoice.Type, invoice.Status);

        if (direction == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        return invoice.Lines
            .Where(line => line.ProduitId.HasValue)
            .GroupBy(line => line.ProduitId!.Value)
            .ToDictionary(
                group => group.Key,
                group => direction * group.Sum(line => line.Quantity));
    }

    public static async Task<Result<Updated>> ApplyDeltaAsync(
        IAppDbContext context,
        IReadOnlyDictionary<Guid, decimal> before,
        IReadOnlyDictionary<Guid, decimal> after,
        CancellationToken ct)
    {
        var productIds = before.Keys
            .Concat(after.Keys)
            .Distinct()
            .ToList();

        if (productIds.Count == 0)
        {
            return Result.Updated;
        }

        var products = context.Produits.Local
            .Where(product => productIds.Contains(product.Id))
            .ToDictionary(product => product.Id);

        var missingProductIds = productIds.Where(id => !products.ContainsKey(id)).ToList();
        if (missingProductIds.Count > 0)
        {
            var persistedProducts = await context.Produits
                .Where(product => missingProductIds.Contains(product.Id))
                .ToListAsync(ct);

            foreach (var product in persistedProducts)
            {
                products[product.Id] = product;
            }
        }

        if (products.Count != productIds.Count)
        {
            return ProduitErrors.NotFound;
        }

        foreach (var productId in productIds)
        {
            before.TryGetValue(productId, out var beforeQuantity);
            after.TryGetValue(productId, out var afterQuantity);
            var delta = afterQuantity - beforeQuantity;

            if (delta == 0)
            {
                continue;
            }

            var adjustment = products[productId].AdjustQuantity(delta);

            if (adjustment.IsError)
            {
                return adjustment.Errors;
            }
        }

        return Result.Updated;
    }

    private static decimal GetDirection(
        InvoiceNature nature,
        InvoiceType type,
        InvoiceStatus status)
    {
        if (status == InvoiceStatus.Cancelled)
        {
            return 0;
        }

        if (nature == InvoiceNature.Achat)
        {
            return type switch
            {
                InvoiceType.BonReception => 1,
                InvoiceType.Facture => 0,
                InvoiceType.Avoir => -1,
                _ => 0
            };
        }


        return type switch
        {
            InvoiceType.BonLivraison => -1,
            InvoiceType.Avoir => 1,
            _ => 0
        };
    }
}
