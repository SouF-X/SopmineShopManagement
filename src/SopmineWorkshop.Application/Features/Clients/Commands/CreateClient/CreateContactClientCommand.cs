using MediatR;

using SopmineWorkshop.Application.Features.Clients.Dtos;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Enums;

namespace SopmineWorkshop.Application.Features.Clients.Commands.CreateClient;

public sealed record CreateContactClientCommand(
    string Nom,
    string Tel,
    ContactClientRole Role
) : IRequest<Result<ContactClientDto>>;