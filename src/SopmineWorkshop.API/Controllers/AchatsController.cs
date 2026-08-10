using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using SopmineWorkshop.API.Common;
using SopmineWorkshop.Application.Features.Invoices.Commands.ExtractInvoiceFromImage;
using SopmineWorkshop.Domain.Enums;
using SopmineWorkshop.Domain.Invoices;

namespace SopmineWorkshop.API.Controllers;

[Authorize(Policy = ApiAuthorizationPolicies.PurchasesOnly)]
[Route("api/v{version:apiVersion}/achats")]
[ApiVersion("1.0")]
public sealed class AchatsController(ISender sender) : ApiController
{
    [HttpPost("extract-from-image")]
    [MapToApiVersion("1.0")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(InvoiceExtractionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExtractFromImage(
        [FromForm] IFormFile? image,
        [FromForm] InvoiceType type = InvoiceType.Facture,
        CancellationToken ct = default)
    {
        if (image is null)
        {
            return Problem([InvoiceExtractionErrors.ImageRequired]);
        }

        await using var stream = new MemoryStream();
        await image.CopyToAsync(stream, ct);

        var command = new ExtractInvoiceFromImageCommand(
            stream.ToArray(),
            image.ContentType,
            image.FileName,
            type);

        var result = await sender.Send(command, ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }
}
