using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

using SopmineWorkshop.API.Common;
using SopmineWorkshop.Application.Features.Familles.Commands.CreateFamille;
using SopmineWorkshop.Application.Features.Familles.Commands.DeleteFamille;
using SopmineWorkshop.Application.Features.Familles.Commands.UpdateFamille;
using SopmineWorkshop.Application.Features.Familles.Dtos;
using SopmineWorkshop.Application.Features.Familles.Queries.GetFamilles;
using SopmineWorkshop.Contracts.Requests.Familles;

namespace SopmineWorkshop.API.Controllers;

[Route("api/v{version:apiVersion}/familles")]
[ApiVersion("1.0")]
public sealed class FamillesController(ISender sender, IOutputCacheStore cacheStore) : ApiController
{
    [HttpGet]
    [MapToApiVersion("1.0")]
    [OutputCache(PolicyName = ApiCachePolicies.ReferenceData, Tags = new[] { ApiCacheTags.Familles })]
    [ProducesResponseType(typeof(List<FamilleProduitDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await sender.Send(new GetFamillesQuery(), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpPost]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(FamilleProduitDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateFamilleRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateFamilleCommand(request.Libelle), ct);

        if (result.IsSuccess)
        {
            await cacheStore.EvictByTagAsync(ApiCacheTags.Familles, ct);
        }

        return result.Match(
            response => Created("/api/v1/familles", response),
            Problem);
    }

    [HttpPut("{familleId:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(Guid familleId, [FromBody] UpdateFamilleRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateFamilleCommand(familleId, request.Libelle), ct);

        if (result.IsSuccess)
        {
            await cacheStore.EvictByTagAsync(ApiCacheTags.Familles, ct);
        }

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpDelete("{familleId:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid familleId, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteFamilleCommand(familleId), ct);

        if (result.IsSuccess)
        {
            await cacheStore.EvictByTagAsync(ApiCacheTags.Familles, ct);
        }

        return result.Match(
            _ => NoContent(),
            Problem);
    }
}
