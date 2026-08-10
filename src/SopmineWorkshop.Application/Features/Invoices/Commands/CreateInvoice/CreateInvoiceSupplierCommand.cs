namespace SopmineWorkshop.Application.Features.Invoices.Commands.CreateInvoice;

public sealed record CreateInvoiceSupplierCommand(
    string? Name,
    string? ICE,
    string? Address,
    string? City,
    string? Phone,
    string? Email,
    string? Website);
