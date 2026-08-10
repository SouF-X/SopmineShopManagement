using FluentValidation;

using SopmineWorkshop.Domain.Enums;

namespace SopmineWorkshop.Application.Features.Invoices.Commands.ExtractInvoiceFromImage;

public sealed class ExtractInvoiceFromImageCommandValidator : AbstractValidator<ExtractInvoiceFromImageCommand>
{
    private const int MaxImageSizeInBytes = 20 * 1024 * 1024;

    public ExtractInvoiceFromImageCommandValidator()
    {
        RuleFor(x => x.ImageBytes)
            .Must(image => image is { Length: > 0 })
            .WithMessage("L'image de la facture est obligatoire.")
            .Must(image => image is null || image.Length <= MaxImageSizeInBytes)
            .WithMessage("L'image ne peut pas depasser 20 Mo.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("Le type du fichier image est obligatoire.")
            .Must(IsImageContentType).WithMessage("Le fichier doit etre une image valide.");

        RuleFor(x => x.Type)
            .Must(type => type is InvoiceType.BonReception or InvoiceType.Facture)
            .WithMessage("Le type de document doit etre un bon de reception ou une facture fournisseur.");
    }

    private static bool IsImageContentType(string? contentType)
    {
        return !string.IsNullOrWhiteSpace(contentType)
            && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }
}
