namespace SopmineWorkshop.Application.Features.Statements.Dtos;
public sealed class PartyStatementDto
{
    public Guid PartyId { get; init; }
    public string PartyName { get; init; } = string.Empty;
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public decimal OpeningBalance { get; init; }
    public decimal TotalInvoiced { get; init; }
    public decimal TotalCredits { get; init; }
    public decimal TotalPaid { get; init; }
    public decimal RemainingBalance { get; init; }
    public decimal OverdueAmount { get; init; }
    public List<StatementMovementDto> Movements { get; init; } = [];
}
