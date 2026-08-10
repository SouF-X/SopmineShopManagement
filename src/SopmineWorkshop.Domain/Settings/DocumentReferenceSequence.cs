namespace SopmineWorkshop.Domain.Settings;

public sealed class DocumentReferenceSequence
{
    public string Scope { get; private set; } = string.Empty;
    public long LastSequence { get; private set; }

    private DocumentReferenceSequence() { }

    public DocumentReferenceSequence(string scope, long lastSequence = 0)
    {
        Scope = scope;
        LastSequence = lastSequence;
    }

    public void SetLastSequence(long lastSequence) => LastSequence = lastSequence;
}
