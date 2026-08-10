using SopmineWorkshop.Domain.Common;
using SopmineWorkshop.Domain.Enums;

namespace SopmineWorkshop.Domain.Settings;

public sealed class DocumentNomination : AuditableEntity
{
    public InvoiceNature Nature { get; private set; }
    public InvoiceType Type { get; private set; }
    public string Root { get; private set; } = string.Empty;
    public string DateFormat { get; private set; } = "MM";
    public int IncrementSize { get; private set; } = 3;

    private DocumentNomination() { }

    public DocumentNomination(
        Guid id,
        InvoiceNature nature,
        InvoiceType type,
        string root,
        string dateFormat,
        int incrementSize) : base(id)
    {
        Nature = nature;
        Type = type;
        Update(root, dateFormat, incrementSize);
    }

    public void Update(string root, string dateFormat, int incrementSize)
    {
        Root = root.Trim();
        DateFormat = string.IsNullOrWhiteSpace(dateFormat) ? "MM" : dateFormat.Trim();
        IncrementSize = Math.Clamp(incrementSize, 1, 8);
    }
}
