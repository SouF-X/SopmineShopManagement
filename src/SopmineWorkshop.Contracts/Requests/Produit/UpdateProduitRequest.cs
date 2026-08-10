namespace SopmineWorkshop.Contracts.Requests.Produits;

public sealed class UpdateProduitRequest
{
    public string Reference { get; set; } = string.Empty;

    public string Nom { get; set; } = string.Empty;

    public string Famille { get; set; } = string.Empty;

    public string Unite { get; set; } = string.Empty;

    public Guid? FournisseurId { get; set; }

    public string? ImageUrl { get; set; }

    public decimal Quantite { get; set; }

    public decimal QuantiteMini { get; set; }

    public decimal PuAchatHT { get; set; }

    public decimal TVA { get; set; }

    public decimal Marge { get; set; }

    public decimal PVenteTTC { get; set; }
}
