using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

using SopmineWorkshop.API.Common;
using SopmineWorkshop.Application.Features.Produits.Commands.CreateProduit;
using SopmineWorkshop.Application.Features.Produits.Commands.DeleteProduit;
using SopmineWorkshop.Application.Features.Produits.Commands.UpdateProduit;
using SopmineWorkshop.Application.Features.Produits.Dtos;
using SopmineWorkshop.Application.Features.Produits.Queries.GetProduitById;
using SopmineWorkshop.Application.Features.Produits.Queries.GetProduits;
using SopmineWorkshop.Contracts.Requests.Produits;

namespace SopmineWorkshop.API.Controllers;

[Route("api/v{version:apiVersion}/produits")]
[ApiVersion("1.0")]
public sealed class ProduitsController(ISender sender, IOutputCacheStore cacheStore) : ApiController
{
    [HttpGet]
    [MapToApiVersion("1.0")]
    [OutputCache(PolicyName = ApiCachePolicies.BusinessList, Tags = new[] { ApiCacheTags.Produits })]
    [ProducesResponseType(typeof(List<ProduitDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await sender.Send(new GetProduitsQuery(), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpGet("{produitId:guid}", Name = "GetProduitById")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ProduitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid produitId, CancellationToken ct)
    {
        var result = await sender.Send(new GetProduitByIdQuery(produitId), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpPost]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ProduitDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateProduitRequest request, CancellationToken ct)
    {
        var command = new CreateProduitCommand(
            request.Reference,
            request.Nom,
            request.Famille,
            request.Unite,
            request.FournisseurId,
            request.ImageUrl,
            request.Quantite,
            request.QuantiteMini,
            request.PuAchatHT,
            request.TVA,
            request.Marge,
            request.PVenteTTC);

        var result = await sender.Send(command, ct);

        if (result.IsSuccess)
        {
            await cacheStore.EvictByTagAsync(ApiCacheTags.Produits, ct);
        }

        return result.Match(
            response => CreatedAtRoute(
                routeName: "GetProduitById",
                routeValues: new { version = "1.0", produitId = response.ProduitId },
                value: response),
            Problem);
    }

    [HttpPut("{produitId:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(Guid produitId, [FromBody] UpdateProduitRequest request, CancellationToken ct)
    {
        var command = new UpdateProduitCommand(
            produitId,
            request.Reference,
            request.Nom,
            request.Famille,
            request.Unite,
            request.FournisseurId,
            request.ImageUrl,
            request.Quantite,
            request.QuantiteMini,
            request.PuAchatHT,
            request.TVA,
            request.Marge,
            request.PVenteTTC);

        var result = await sender.Send(command, ct);

        if (result.IsSuccess)
        {
            await cacheStore.EvictByTagAsync(ApiCacheTags.Produits, ct);
        }

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpDelete("{produitId:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid produitId, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteProduitCommand(produitId), ct);

        if (result.IsSuccess)
        {
            await cacheStore.EvictByTagAsync(ApiCacheTags.Produits, ct);
        }

        return result.Match(
            _ => NoContent(),
            Problem);
    }
}
