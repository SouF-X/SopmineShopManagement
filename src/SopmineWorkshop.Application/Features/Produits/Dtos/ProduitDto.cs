namespace SopmineWorkshop.Application.Features.Produits.Dtos;

public class ProduitDto
{
    public Guid ProduitId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public string Famille { get; set; } = string.Empty;
    public string Unite { get; set; } = string.Empty;
    public Guid? FournisseurId { get; set; }
    public string? FournisseurNom { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Quantite { get; set; }
    public decimal QuantiteMini { get; set; }
    public decimal PuAchatHT { get; set; }
    public decimal TVA { get; set; }
    public decimal Marge { get; set; }
    public decimal PVenteTTC { get; set; }
}
