using FluentValidation;

namespace SopmineWorkshop.Application.Features.Fournisseurs.Commands.CreateFournisseur;

public sealed class CreateFournisseurCommandValidator : AbstractValidator<CreateFournisseurCommand>
{
    public CreateFournisseurCommandValidator()
    {
    }

    private static bool BeAValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
