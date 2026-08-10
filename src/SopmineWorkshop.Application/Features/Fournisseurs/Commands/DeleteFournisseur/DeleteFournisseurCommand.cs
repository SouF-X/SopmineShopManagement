using MediatR;

using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Application.Features.Fournisseurs.Commands.DeleteFournisseur;

public sealed record DeleteFournisseurCommand(Guid FournisseurId) : IRequest<Result<Deleted>>;
