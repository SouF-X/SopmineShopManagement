using MediatR;

using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Produits.Commands.UpdateProduit;

public sealed record UpdateProduitCommand(
    Guid ProduitId,
    string Reference,
    string Nom,
    string Famille,
    string Unite,
    Guid? FournisseurId,
    string? ImageUrl,
    decimal Quantite,
    decimal QuantiteMini,
    decimal PuAchatHT,
    decimal TVA,
    decimal Marge,
    decimal PVenteTTC
) : IRequest<Result<Updated>>;
