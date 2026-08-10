using SopmineWorkshop.Domain.Enums;

namespace SopmineWorkshop.Domain.Settings;

public sealed record DocumentNominationDefinition(
    string Key,
    InvoiceNature Nature,
    InvoiceType Type,
    string DefaultCode);

public static class DocumentNominationCatalog
{
    public static readonly IReadOnlyList<DocumentNominationDefinition> Definitions =
    [
        new("achat:boncommande", InvoiceNature.Achat, InvoiceType.BonCommande, "BC"),
        new("achat:bonreception", InvoiceNature.Achat, InvoiceType.BonReception, "BR-A"),
        new("achat:facture", InvoiceNature.Achat, InvoiceType.Facture, "FA-A"),
        new("achat:avoir", InvoiceNature.Achat, InvoiceType.Avoir, "AV-A"),
        new("vente:devis", InvoiceNature.Vente, InvoiceType.Devis, "DV"),
        new("vente:bonlivraison", InvoiceNature.Vente, InvoiceType.BonLivraison, "BL"),
        new("vente:facture", InvoiceNature.Vente, InvoiceType.Facture, "FA"),
        new("vente:avoir", InvoiceNature.Vente, InvoiceType.Avoir, "AV"),
    ];

    public static DocumentNominationDefinition? Find(InvoiceNature nature, InvoiceType type) =>
        Definitions.FirstOrDefault(item => item.Nature == nature && item.Type == type);

    public static DocumentNominationDefinition? Find(string key) =>
        Definitions.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));

    public static string DefaultRoot(DocumentNominationDefinition definition, DateTime now) =>
        $"{now:yy}{definition.DefaultCode}";
}
