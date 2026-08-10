using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

using SopmineWorkshop.API.Common;
using SopmineWorkshop.Application.Features.Fournisseurs.Commands.CreateFournisseur;
using SopmineWorkshop.Application.Features.Fournisseurs.Dtos;
using SopmineWorkshop.Application.Features.Fournisseurs.Queries.GetFournisseurById;
using SopmineWorkshop.Application.Features.Fournisseurs.Queries.GetFournisseurs;
using SopmineWorkshop.Application.Features.Fournisseurs.Commands.UpdateFournisseur;
using SopmineWorkshop.Contracts.Requests.Fournisseurs;
using SopmineWorkshop.Application.Features.Fournisseurs.Commands.DeleteFournisseur;
using SopmineWorkshop.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using SopmineWorkshop.Application.Features.Statements;
using SopmineWorkshop.Application.Features.Statements.Dtos;
using SopmineWorkshop.Application.Features.Statements.Queries.GetPartyStatement;

namespace SopmineWorkshop.API.Controllers;

[Route("api/v{version:apiVersion}/fournisseurs")]
[ApiVersion("1.0")]
public sealed class FournisseursController(ISender sender, IOutputCacheStore cacheStore) : ApiController
{

    //  / /////////////////////////// Get All Fournisseurs ////////////////////////////////////////////////////////////////////////////////////

    [HttpGet]
    [MapToApiVersion("1.0")]
    [OutputCache(PolicyName = ApiCachePolicies.BusinessList, Tags = new[] { ApiCacheTags.Fournisseurs })]
    [ProducesResponseType(typeof(List<FournisseurDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await sender.Send(new GetFournisseursQuery(), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    //  / /////////////////////////// Get Fournisseur By ID ////////////////////////////////////////////////////////////////////////////////////

    [HttpGet("{fournisseurId:guid}", Name = "GetFournisseurById")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(FournisseurDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid fournisseurId, CancellationToken ct)
    {
        var result = await sender.Send(new GetFournisseurByIdQuery(fournisseurId), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    //  / /////////////////////////// Create Fournisseur ////////////////////////////////////////////////////////////////////////////////////

    [HttpPost]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(FournisseurDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateFournisseurRequest request, CancellationToken ct)
    {
        var contacts = (request.Contacts ?? [])
            .ConvertAll(c => new CreateContactFournisseurCommand(
                c.Nom,
                c.Tel,
                (ContactRole)c.Role));

        var command = new CreateFournisseurCommand(
            request.Nom,
            request.ICE,
            request.Adresse,
            request.Ville,
            request.TelFix,
            request.SiteWeb,
            request.Email,
            contacts);

        var result = await sender.Send(command, ct);

        if (result.IsSuccess)
        {
            await cacheStore.EvictByTagAsync(ApiCacheTags.Fournisseurs, ct);
        }

        return result.Match(
            response => CreatedAtRoute(
                routeName: "GetFournisseurById",
                routeValues: new { version = "1.0", fournisseurId = response.FournisseurId },
                value: response),
            Problem);
    }

    //  / /////////////////////////// Update Fournisseur ////////////////////////////////////////////////////////////////////////////////////

    [HttpPut("{fournisseurId:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(Guid fournisseurId, [FromBody] UpdateFournisseurRequest request, CancellationToken ct)
    {
        var contacts = (request.Contacts ?? [])
        .ConvertAll(c => new UpdateContactFournisseurCommand(
                c.ContactFournisseurId,
                c.Nom,
                c.Tel,
                (ContactRole)c.Role));

        var command = new UpdateFournisseurCommand(
            fournisseurId,
            request.Nom,
            request.ICE,
            request.Adresse,
            request.Ville,
            request.TelFix,
            request.SiteWeb,
            request.Email,
            contacts);

        var result = await sender.Send(command, ct);

        if (result.IsSuccess)
        {
            await cacheStore.EvictByTagAsync(ApiCacheTags.Fournisseurs, ct);
            await cacheStore.EvictByTagAsync(ApiCacheTags.Produits, ct);
        }

        return result.Match(
            response => Ok(response),
            Problem);
    }

    //  / /////////////////////////// Delete Fournisseur ////////////////////////////////////////////////////////////////////////////////////

    [HttpDelete("{fournisseurId:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid fournisseurId, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteFournisseurCommand(fournisseurId), ct);

        if (result.IsSuccess)
        {
            await cacheStore.EvictByTagAsync(ApiCacheTags.Fournisseurs, ct);
        }

        return result.Match(
            _ => NoContent(),
            Problem);
    }

    [HttpGet("{fournisseurId:guid}/statement")]
    [Authorize(Policy = ApiAuthorizationPolicies.PurchasesOnly)]
    [ProducesResponseType(typeof(PartyStatementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatement(Guid fournisseurId, DateTime? from, DateTime? to, InvoicePaymentProgress? paymentProgress, CancellationToken ct)
    {
        var result = await sender.Send(new GetPartyStatementQuery(StatementPartyKind.Fournisseur, fournisseurId, from, to, paymentProgress), ct);
        return result.Match(Ok, Problem);
    }

}
