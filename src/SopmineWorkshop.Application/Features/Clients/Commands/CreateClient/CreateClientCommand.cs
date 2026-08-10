using MediatR;

using SopmineWorkshop.Application.Features.Clients.Dtos;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Enums;

namespace SopmineWorkshop.Application.Features.Clients.Commands.CreateClient;

public sealed record CreateClientCommand(
    string Nom,
    ClientType Type,
    string? ICE,
    string? Adresse,
    string? Ville,
    string Tel,
    List<CreateContactClientCommand> Contacts
) : IRequest<Result<ClientDto>>;
