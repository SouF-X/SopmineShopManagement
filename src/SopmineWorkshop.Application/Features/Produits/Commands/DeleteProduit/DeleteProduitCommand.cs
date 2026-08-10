using MediatR;

using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Produits.Commands.DeleteProduit;

public sealed record DeleteProduitCommand(Guid ProduitId) : IRequest<Result<Deleted>>;
