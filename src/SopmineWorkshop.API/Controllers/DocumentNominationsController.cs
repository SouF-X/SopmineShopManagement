using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

using SopmineWorkshop.API.Common;
using SopmineWorkshop.Application.Features.DocumentNominations.Commands.UpdateDocumentNomination;
using SopmineWorkshop.Application.Features.DocumentNominations.Queries.GetDocumentNominations;
using SopmineWorkshop.Contracts.Requests.DocumentNominations;
using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Identity;

namespace SopmineWorkshop.API.Controllers;

[Route("api/v{version:apiVersion}/document-nominations")]
[ApiVersion("1.0")]
public sealed class DocumentNominationsController(
    ISender sender,
    IOutputCacheStore cacheStore) : ApiController
{
    [HttpGet]
    [MapToApiVersion("1.0")]
    [OutputCache(PolicyName = ApiCachePolicies.ReferenceData, Tags = new[] { ApiCacheTags.DocumentNominations })]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await sender.Send(
            new GetDocumentNominationsQuery(PurchaseAccess.CanAccessPurchases(User)),
            ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [Authorize(Roles = nameof(Role.Admin))]
    [HttpPut("{key}")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Update(
        string key,
        [FromBody] UpdateDocumentNominationRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateDocumentNominationCommand(
                key,
                request.Root,
                request.DateFormat,
                request.IncrementSize,
                PurchaseAccess.CanAccessPurchases(User)),
            ct);

        if (result.IsSuccess)
        {
            await cacheStore.EvictByTagAsync(ApiCacheTags.DocumentNominations, ct);
        }

        return result.Match(
            response => Ok(response),
            MapErrors);
    }

    private ActionResult MapErrors(List<Error> errors)
    {
        if (errors.Any(error => error.Type == ErrorKind.NotFound))
        {
            return NotFound();
        }

        if (errors.Any(error => error.Type == ErrorKind.Forbidden))
        {
            return Forbid();
        }

        return Problem(errors);
    }
}
