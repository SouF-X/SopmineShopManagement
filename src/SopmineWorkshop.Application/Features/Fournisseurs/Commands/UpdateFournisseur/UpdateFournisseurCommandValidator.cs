using FluentValidation;

namespace SopmineWorkshop.Application.Features.Fournisseurs.Commands.UpdateFournisseur;

public sealed class UpdateFournisseurCommandValidator : AbstractValidator<UpdateFournisseurCommand>
{
    public UpdateFournisseurCommandValidator()
    {
    }

    private static bool BeAValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
