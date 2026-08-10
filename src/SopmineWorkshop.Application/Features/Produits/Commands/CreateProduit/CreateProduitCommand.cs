using MediatR;

using SopmineWorkshop.Application.Features.Produits.Dtos;
using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Produits.Commands.CreateProduit;

public sealed record CreateProduitCommand(
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
) : IRequest<Result<ProduitDto>>;
