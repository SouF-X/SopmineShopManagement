using MediatR;

using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Enums;

namespace SopmineWorkshop.Application.Features.Clients.Commands.UpdateClient;

public sealed record UpdateClientCommand(
    Guid ClientId,
    string Nom,
    ClientType Type,
    string? ICE,
    string? Adresse,
    string? Ville,
    string Tel,
    List<UpdateContactClientCommand> Contacts
) : IRequest<Result<Updated>>;
