using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

using SopmineWorkshop.API.Common;
using SopmineWorkshop.Application.Features.UnitesMesure.Commands.CreateUniteMesure;
using SopmineWorkshop.Application.Features.UnitesMesure.Commands.DeleteUniteMesure;
using SopmineWorkshop.Application.Features.UnitesMesure.Commands.UpdateUniteMesure;
using SopmineWorkshop.Application.Features.UnitesMesure.Dtos;
using SopmineWorkshop.Application.Features.UnitesMesure.Queries.GetUnitesMesure;
using SopmineWorkshop.Contracts.Requests.UnitesMesure;

namespace SopmineWorkshop.API.Controllers;

[Route("api/v{version:apiVersion}/unites-mesure")]
[ApiVersion("1.0")]
public sealed class UnitesMesureController(ISender sender, IOutputCacheStore cacheStore) : ApiController
{
    [HttpGet]
    [MapToApiVersion("1.0")]
    [OutputCache(PolicyName = ApiCachePolicies.ReferenceData, Tags = new[] { ApiCacheTags.UnitesMesure })]
    [ProducesResponseType(typeof(List<UniteMesureDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await sender.Send(new GetUnitesMesureQuery(), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpPost]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(UniteMesureDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateUniteMesureRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateUniteMesureCommand(request.Libelle), ct);

        if (result.IsSuccess)
        {
            await cacheStore.EvictByTagAsync(ApiCacheTags.UnitesMesure, ct);
        }

        return result.Match(
            response => Created("/api/v1/unites-mesure", response),
            Problem);
    }

    [HttpPut("{uniteMesureId:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(Guid uniteMesureId, [FromBody] UpdateUniteMesureRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateUniteMesureCommand(uniteMesureId, request.Libelle), ct);

        if (result.IsSuccess)
        {
            await cacheStore.EvictByTagAsync(ApiCacheTags.UnitesMesure, ct);
        }

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpDelete("{uniteMesureId:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid uniteMesureId, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteUniteMesureCommand(uniteMesureId), ct);

        if (result.IsSuccess)
        {
            await cacheStore.EvictByTagAsync(ApiCacheTags.UnitesMesure, ct);
        }

        return result.Match(
            _ => NoContent(),
            Problem);
    }
}
