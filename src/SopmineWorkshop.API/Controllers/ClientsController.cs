using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

using SopmineWorkshop.API.Common;
using SopmineWorkshop.Application.Features.Clients.Commands.CreateClient;
using SopmineWorkshop.Application.Features.Clients.Commands.DeleteClient;
using SopmineWorkshop.Application.Features.Clients.Commands.UpdateClient;
using SopmineWorkshop.Application.Features.Clients.Dtos;
using SopmineWorkshop.Application.Features.Clients.Queries.GetClientById;
using SopmineWorkshop.Application.Features.Clients.Queries.GetClients;
using SopmineWorkshop.Contracts.Requests.Clients;
using SopmineWorkshop.Domain.Enums;
using SopmineWorkshop.Application.Features.Statements;
using SopmineWorkshop.Application.Features.Statements.Dtos;
using SopmineWorkshop.Application.Features.Statements.Queries.GetPartyStatement;

namespace SopmineWorkshop.API.Controllers;

[Route("api/v{version:apiVersion}/clients")]
[ApiVersion("1.0")]
public sealed class ClientsController(ISender sender, IOutputCacheStore cacheStore) : ApiController
{
    [HttpGet]
    [MapToApiVersion("1.0")]
    [OutputCache(PolicyName = ApiCachePolicies.BusinessList, Tags = new[] { ApiCacheTags.Clients })]
    [ProducesResponseType(typeof(List<ClientDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await sender.Send(new GetClientsQuery(), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpGet("{clientId:guid}", Name = "GetClientById")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid clientId, CancellationToken ct)
    {
        var result = await sender.Send(new GetClientByIdQuery(clientId), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpPost]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateClientRequest request, CancellationToken ct)
    {
        var contacts = (request.Contacts ?? [])
            .ConvertAll(c => new CreateContactClientCommand(
                c.Nom,
                c.Tel,
                (ContactClientRole)c.Role));

        var command = new CreateClientCommand(
            request.Nom,
            (ClientType)request.Type,
            request.ICE,
            request.Adresse,
            request.Ville,
            request.Tel,
            contacts);

        var result = await sender.Send(command, ct);

        if (result.IsSuccess)
        {
            await cacheStore.EvictByTagAsync(ApiCacheTags.Clients, ct);
        }

        return result.Match(
            response => CreatedAtRoute(
                routeName: "GetClientById",
                routeValues: new { version = "1.0", clientId = response.ClientId },
                value: response),
            Problem);
    }

    [HttpPut("{clientId:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(Guid clientId, [FromBody] UpdateClientRequest request, CancellationToken ct)
    {
        var contacts = (request.Contacts ?? [])
            .ConvertAll(c => new UpdateContactClientCommand(
                c.ContactClientId ?? Guid.NewGuid(),
                c.Nom,
                c.Tel,
                (ContactClientRole)c.Role));

        var command = new UpdateClientCommand(
            clientId,
            request.Nom,
            (ClientType)request.Type,
            request.ICE,
            request.Adresse,
            request.Ville,
            request.Tel,
            contacts);

        var result = await sender.Send(command, ct);

        if (result.IsSuccess)
        {
            await cacheStore.EvictByTagAsync(ApiCacheTags.Clients, ct);
        }

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpDelete("{clientId:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid clientId, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteClientCommand(clientId), ct);

        if (result.IsSuccess)
        {
            await cacheStore.EvictByTagAsync(ApiCacheTags.Clients, ct);
        }

        return result.Match(
            _ => NoContent(),
            Problem);
    }
    [HttpGet("{clientId:guid}/statement")]
    [ProducesResponseType(typeof(PartyStatementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatement(Guid clientId, DateTime? from, DateTime? to, InvoicePaymentProgress? paymentProgress, CancellationToken ct)
    {
        var result = await sender.Send(new GetPartyStatementQuery(StatementPartyKind.Client, clientId, from, to, paymentProgress), ct);
        return result.Match(Ok, Problem);
    }

}
