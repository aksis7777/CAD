namespace MiniPdm.Contracts.Modules.Composition.DtoModels;

public sealed record CompositionItemDto(Guid ChildObjectId, int Quantity);

public sealed record ReplaceCompositionRequestDto(IReadOnlyList<CompositionItemDto>? Components,
    Guid ExpectedConcurrencyToken);
