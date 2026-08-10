using SopmineWorkshop.Domain.Settings;

namespace SopmineWorkshop.Application.Features.DocumentNominations.Dtos;

public static class DocumentNominationMapper
{
    private static readonly IReadOnlyDictionary<string, (string Label, string Icon)> Presentation =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["achat:boncommande"] = ("Bon de commande achat", "assignment"),
            ["achat:bonreception"] = ("Bon de réception", "inventory"),
            ["achat:facture"] = ("Facture fournisseur", "receipt_long"),
            ["achat:avoir"] = ("Avoir fournisseur", "credit_score"),
            ["vente:devis"] = ("Devis", "request_quote"),
            ["vente:bonlivraison"] = ("Bon de livraison", "local_shipping"),
            ["vente:facture"] = ("Facture", "receipt_long"),
            ["vente:avoir"] = ("Avoir", "credit_score"),
        };

    public static DocumentNominationDto ToDto(
        DocumentNominationDefinition definition,
        DocumentNomination? saved)
    {
        var presentation = Presentation[definition.Key];

        return new DocumentNominationDto(
            definition.Key,
            (int)definition.Nature,
            (int)definition.Type,
            presentation.Label,
            presentation.Icon,
            string.IsNullOrWhiteSpace(saved?.Root)
                ? DocumentNominationCatalog.DefaultRoot(definition, DateTime.UtcNow)
                : saved.Root.Trim(),
            saved?.DateFormat ?? "MM",
            saved?.IncrementSize ?? 3);
    }
}
