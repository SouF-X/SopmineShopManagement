using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

using SopmineWorkshop.API.Common;
using SopmineWorkshop.Application.Features.Invoices.Commands.CreateInvoice;
using SopmineWorkshop.Application.Features.Invoices.Commands.ConvertBonLivraisons;
using SopmineWorkshop.Application.Features.Invoices.Commands.DeleteInvoice;
using SopmineWorkshop.Application.Features.Invoices.Commands.UpdateInvoice;
using SopmineWorkshop.Application.Features.InvoicePayments.Commands.RecordInvoicePayment;
using SopmineWorkshop.Application.Features.InvoicePayments.Commands.CancelInvoicePayment;
using SopmineWorkshop.Application.Features.InvoicePayments.Dtos;
using SopmineWorkshop.Application.Features.InvoicePayments.Queries.GetInvoicePayments;
using SopmineWorkshop.Application.Features.Invoices.Dtos;
using SopmineWorkshop.Application.Features.Invoices.Queries.GetInvoiceById;
using SopmineWorkshop.Application.Features.Invoices.Queries.GetInvoices;
using SopmineWorkshop.Contracts.Requests.Invoices;
using SopmineWorkshop.Contracts.Requests.Invoice;
using SopmineWorkshop.Domain.Enums;

namespace SopmineWorkshop.API.Controllers;

[Route("api/v{version:apiVersion}/invoices")]
[ApiVersion("1.0")]
public sealed class InvoicesController(ISender sender, IOutputCacheStore cacheStore) : ApiController
{
    [HttpGet]
    [MapToApiVersion("1.0")]
    [OutputCache(PolicyName = ApiCachePolicies.BusinessList, Tags = new[] { ApiCacheTags.Invoices })]
    [ProducesResponseType(typeof(List<InvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await sender.Send(new GetInvoicesQuery(), ct);

        return result.Match(
            response =>
            {
                var visibleInvoices = PurchaseAccess.CanAccessPurchases(User)
                    ? response
                    : response.Where(invoice => invoice.Nature != InvoiceNature.Achat).ToList();

                return Ok(visibleInvoices);
            },
            Problem);
    }

    [HttpGet("{invoiceId:guid}", Name = "GetInvoiceById")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid invoiceId, CancellationToken ct)
    {
        var result = await sender.Send(new GetInvoiceByIdQuery(invoiceId), ct);

        return result.Match(
            response => PurchaseAccess.IsRestricted(User, response.Nature)
                ? Forbid()
                : Ok(response),
            Problem);
    }

    [HttpPost]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceRequest request, CancellationToken ct)
    {
        if (PurchaseAccess.IsRestricted(User, (InvoiceNature)request.Nature))
        {
            return Forbid();
        }

        var lines = (request.Lines ?? [])
            .ConvertAll(line => new CreateInvoiceLineCommand(
                line.ProduitId,
                line.ProductReference,
                line.ProductName,
                line.ProductFamily,
                line.ProductUnit,
                line.Quantity,
                line.Price,
                line.PriceTTC,
                line.TVA));

        var command = new CreateInvoiceCommand(
            request.Reference,
            (InvoiceType)request.Type,
            (InvoiceNature)request.Nature,
            request.Date,
            request.DueDate,
            request.FournisseurId,
            request.ClientId,
            request.Status.HasValue ? (SopmineWorkshop.Domain.Enums.InvoiceStatus?)(int)request.Status.Value : null,
            null,
            null,
            request.Notes,
            request.Total,
            request.NewSupplier is null
                ? null
                : new CreateInvoiceSupplierCommand(
                    request.NewSupplier.Name,
                    request.NewSupplier.ICE,
                    request.NewSupplier.Address,
                    request.NewSupplier.City,
                    request.NewSupplier.Phone,
                    request.NewSupplier.Email,
                    request.NewSupplier.Website),
            lines,
            request.CatalogueMode);

        var result = await sender.Send(command, ct);

        if (result.IsSuccess)
        {
            await cacheStore.EvictByTagAsync(ApiCacheTags.Invoices, ct);
            await cacheStore.EvictByTagAsync(ApiCacheTags.Produits, ct);
            await cacheStore.EvictByTagAsync(ApiCacheTags.Fournisseurs, ct);
        }

        return result.Match(
            response => CreatedAtRoute(
                routeName: "GetInvoiceById",
                routeValues: new { version = "1.0", invoiceId = response.InvoiceId },
                value: response),
            Problem);
    }

    [HttpPost("convert-bon-livraisons")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConvertBonLivraisons([FromBody] ConvertBonLivraisonsRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ConvertBonLivraisonsCommand(request.InvoiceIds), ct);

        if (result.IsSuccess)
        {
            await cacheStore.EvictByTagAsync(ApiCacheTags.Invoices, ct);
        }

        return result.Match(
            response => CreatedAtRoute(
                routeName: "GetInvoiceById",
                routeValues: new { version = "1.0", invoiceId = response.InvoiceId },
                value: response),
            Problem);
    }

    [HttpPut("{invoiceId:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(Guid invoiceId, [FromBody] UpdateInvoiceRequest request, CancellationToken ct)
    {
        if (PurchaseAccess.IsRestricted(User, (InvoiceNature)request.Nature))
        {
            return Forbid();
        }

        var existing = await sender.Send(new GetInvoiceByIdQuery(invoiceId), ct);
        if (existing.IsError)
        {
            return Problem(existing.Errors);
        }

        if (PurchaseAccess.IsRestricted(User, existing.Value.Nature))
        {
            return Forbid();
        }

        var lines = (request.Lines ?? [])
            .ConvertAll(line => new UpdateInvoiceLineCommand(
                line.InvoiceLineId,
                line.ProduitId,
                line.ProductReference,
                line.ProductName,
                line.ProductFamily,
                line.ProductUnit,
                line.Quantity,
                line.Price,
                line.PriceTTC,
                line.TVA));

        var command = new UpdateInvoiceCommand(
            invoiceId,
            request.Reference,
            (InvoiceType)request.Type,
            (InvoiceNature)request.Nature,
            request.Date,
            request.DueDate,
            request.FournisseurId,
            request.ClientId,
            request.Status.HasValue ? (SopmineWorkshop.Domain.Enums.InvoiceStatus?)(int)request.Status.Value : null,
            null,
            null,
            request.Notes,
            request.Total,
            lines);

        var result = await sender.Send(command, ct);

        if (result.IsSuccess)
        {
            await cacheStore.EvictByTagAsync(ApiCacheTags.Invoices, ct);
            await cacheStore.EvictByTagAsync(ApiCacheTags.Produits, ct);
        }

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpDelete("{invoiceId:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid invoiceId, CancellationToken ct)
    {
        var existing = await sender.Send(new GetInvoiceByIdQuery(invoiceId), ct);
        if (existing.IsError)
        {
            return Problem(existing.Errors);
        }

        if (PurchaseAccess.IsRestricted(User, existing.Value.Nature))
        {
            return Forbid();
        }

        var result = await sender.Send(new DeleteInvoiceCommand(invoiceId), ct);

        if (result.IsSuccess)
        {
            await cacheStore.EvictByTagAsync(ApiCacheTags.Invoices, ct);
            await cacheStore.EvictByTagAsync(ApiCacheTags.Produits, ct);
        }

        return result.Match(
            _ => NoContent(),
            Problem);
    }
    [HttpGet("{invoiceId:guid}/payments")]
    [ProducesResponseType(typeof(List<InvoicePaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPayments(Guid invoiceId, CancellationToken ct)
    {
        var invoice = await sender.Send(new GetInvoiceByIdQuery(invoiceId), ct);
        if (invoice.IsError) return Problem(invoice.Errors);
        if (PurchaseAccess.IsRestricted(User, invoice.Value.Nature)) return Forbid();
        var result = await sender.Send(new GetInvoicePaymentsQuery(invoiceId), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("{invoiceId:guid}/payments")]
    [ProducesResponseType(typeof(InvoicePaymentMutationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RecordPayment(Guid invoiceId, [FromBody] RecordInvoicePaymentRequest request, CancellationToken ct)
    {
        var invoice = await sender.Send(new GetInvoiceByIdQuery(invoiceId), ct);
        if (invoice.IsError) return Problem(invoice.Errors);
        if (PurchaseAccess.IsRestricted(User, invoice.Value.Nature)) return Forbid();
        var result = await sender.Send(new RecordInvoicePaymentCommand(invoiceId, request.Amount, request.PaymentDate, (SopmineWorkshop.Domain.Enums.InvoicePaymentMethod)request.Method, request.Reference, request.Note), ct);
        if (result.IsSuccess) await EvictPaymentCaches(invoice.Value.ClientId, invoice.Value.FournisseurId, ct);
        return result.Match(x => StatusCode(StatusCodes.Status201Created, x), Problem);
    }

    [HttpPost("{invoiceId:guid}/payments/{paymentId:guid}/cancel")]
    [ProducesResponseType(typeof(InvoicePaymentMutationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelPayment(Guid invoiceId, Guid paymentId, [FromBody] CancelInvoicePaymentRequest request, CancellationToken ct)
    {
        var invoice = await sender.Send(new GetInvoiceByIdQuery(invoiceId), ct);
        if (invoice.IsError) return Problem(invoice.Errors);
        if (PurchaseAccess.IsRestricted(User, invoice.Value.Nature)) return Forbid();
        var result = await sender.Send(new CancelInvoicePaymentCommand(invoiceId, paymentId, request.Reason), ct);
        if (result.IsSuccess) await EvictPaymentCaches(invoice.Value.ClientId, invoice.Value.FournisseurId, ct);
        return result.Match(Ok, Problem);
    }

    private async Task EvictPaymentCaches(Guid? clientId, Guid? fournisseurId, CancellationToken ct)
    {
        await cacheStore.EvictByTagAsync(ApiCacheTags.Invoices, ct);
        await cacheStore.EvictByTagAsync(ApiCacheTags.Clients, ct);
        await cacheStore.EvictByTagAsync(ApiCacheTags.Fournisseurs, ct);
        await cacheStore.EvictByTagAsync(ApiCacheTags.Statements, ct);
    }

}
