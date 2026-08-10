namespace SopmineWorkshop.Contracts.Requests.DocumentNominations;

public sealed class UpdateDocumentNominationRequest
{
    public string Root { get; set; } = string.Empty;
    public string DateFormat { get; set; } = "MM";
    public int IncrementSize { get; set; } = 3;
}
