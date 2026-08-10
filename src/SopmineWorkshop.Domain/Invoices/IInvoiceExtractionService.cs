using SopmineWorkshop.Domain.Common.Results;

namespace SopmineWorkshop.Domain.Invoices;

public interface IInvoiceExtractionService
{
    Task<Result<InvoiceExtractionDto>> ExtractFromImageAsync(
        byte[] imageBytes,
        string contentType,
        string? fileName,
        CancellationToken ct = default);
}
