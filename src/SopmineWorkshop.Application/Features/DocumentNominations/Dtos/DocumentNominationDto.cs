namespace SopmineWorkshop.Application.Features.DocumentNominations.Dtos;

public sealed record DocumentNominationDto(
    string Key,
    int Nature,
    int Type,
    string Label,
    string Icon,
    string Root,
    string DateFormat,
    int IncrementSize);
