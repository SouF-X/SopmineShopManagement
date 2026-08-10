using SopmineWorkshop.Domain.Common;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Fournisseurs;

namespace SopmineWorkshop.Domain.Produits;

public sealed class Produit : AuditableEntity
{
    public string Reference { get; private set; } = string.Empty;
    public string Nom { get; private set; } = string.Empty;
    public string Famille { get; private set; } = string.Empty;
    public string Unite { get; private set; } = string.Empty;
    public Guid? FournisseurId { get; private set; }
    public Fournisseur? Fournisseur { get; private set; }
    public string? ImageUrl { get; private set; }
    public decimal Quantite { get; private set; }
    public decimal QuantiteMini { get; private set; }
    public decimal PuAchatHT { get; private set; }
    public decimal TVA { get; private set; }
    public decimal Marge { get; private set; }
    public decimal PVenteTTC { get; private set; }

    private Produit() { }

    private Produit(Guid id, string reference, string nom, string famille,
        string unite, Guid? fournisseurId, string? imageUrl, decimal quantite, decimal quantiteMini,
        decimal puAchatHT, decimal tva, decimal marge,
        decimal pVenteTTC) : base(id)
    {
        Reference = reference;
        Nom = nom;
        Famille = famille;
        Unite = unite;
        FournisseurId = fournisseurId;
        ImageUrl = NormalizeOptional(imageUrl);
        Quantite = quantite;
        QuantiteMini = quantiteMini;
        PuAchatHT = puAchatHT;
        TVA = tva;
        Marge = marge;
        PVenteTTC = pVenteTTC;
    }

    public static Result<Produit> Create(Guid id, string reference, string nom, string famille,
        string unite, Guid? fournisseurId, string? imageUrl, decimal quantite, decimal quantiteMini,
        decimal puAchatHT, decimal tva, decimal marge,
        decimal pVenteTTC)
    {
        if (fournisseurId.HasValue && fournisseurId.Value == Guid.Empty)
        {
            return ProduitErrors.FournisseurIdInvalid;
        }

        if (quantite < 0)
        {
            return ProduitErrors.QuantityInvalid;
        }

        if (quantiteMini < 0)
        {
            return ProduitErrors.QuantiteMiniInvalid;
        }

        if (puAchatHT < 0)
        {
            return ProduitErrors.PuAchatHTInvalid;
        }

        if (tva < 0)
        {
            return ProduitErrors.TVAInvalid;
        }

        if (marge < 0)
        {
            return ProduitErrors.MargeInvalid;
        }

        if (pVenteTTC < 0)
        {
            return ProduitErrors.PVenteTTCInvalid;
        }

        return new Produit(
            id,
            NormalizeRequired(reference),
            NormalizeRequired(nom),
            NormalizeRequired(famille),
            NormalizeRequired(unite),
            fournisseurId,
            imageUrl,
            quantite,
            quantiteMini,
            puAchatHT,
            tva,
            marge,
            pVenteTTC);
    }

    public Result<Updated> Update(string reference, string nom, string famille,
        string unite, Guid? fournisseurId, string? imageUrl, decimal quantite, decimal quantiteMini,
        decimal puAchatHT, decimal tva, decimal marge,
        decimal pVenteTTC)
    {
        if (fournisseurId.HasValue && fournisseurId.Value == Guid.Empty)
        {
            return ProduitErrors.FournisseurIdInvalid;
        }

        if (quantite < 0)
        {
            return ProduitErrors.QuantityInvalid;
        }

        if (quantiteMini < 0)
        {
            return ProduitErrors.QuantiteMiniInvalid;
        }

        if (puAchatHT < 0)
        {
            return ProduitErrors.PuAchatHTInvalid;
        }

        if (tva < 0)
        {
            return ProduitErrors.TVAInvalid;
        }

        if (marge < 0)
        {
            return ProduitErrors.MargeInvalid;
        }

        if (pVenteTTC < 0)
        {
            return ProduitErrors.PVenteTTCInvalid;
        }

        Reference = NormalizeRequired(reference);
        Nom = NormalizeRequired(nom);
        Famille = NormalizeRequired(famille);
        Unite = NormalizeRequired(unite);
        FournisseurId = fournisseurId;
        ImageUrl = NormalizeOptional(imageUrl);
        Quantite = quantite;
        QuantiteMini = quantiteMini;
        PuAchatHT = puAchatHT;
        TVA = tva;
        Marge = marge;
        PVenteTTC = pVenteTTC;

        return Result.Updated;
    }

    public Result<Updated> AdjustQuantity(decimal delta)
    {
        var nextQuantity = Quantite + delta;

        if (nextQuantity < 0)
        {
            return ProduitErrors.InsufficientStock;
        }

        Quantite = nextQuantity;

        return Result.Updated;
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string NormalizeRequired(string? value)
        => value?.Trim() ?? string.Empty;
}
