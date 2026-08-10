using SopmineWorkshop.Domain.Enums;

namespace SopmineWorkshop.Application.Common.Interfaces;

public interface IDocumentReferenceGenerator
{
    Task<string> GenerateAsync(
        InvoiceNature nature,
        InvoiceType type,
        DateTime documentDate,
        CancellationToken cancellationToken);
}
