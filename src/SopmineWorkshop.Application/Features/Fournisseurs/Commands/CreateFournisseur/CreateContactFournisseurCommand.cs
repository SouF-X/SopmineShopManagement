using MediatR;

using SopmineWorkshop.Application.Features.Fournisseurs.Dtos;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Enums;

namespace SopmineWorkshop.Application.Features.Fournisseurs.Commands.CreateFournisseur;

public sealed record CreateContactFournisseurCommand(
    string Nom,
    string Tel,
    ContactRole Role
) : IRequest<Result<ContactFournisseurDto>>;
