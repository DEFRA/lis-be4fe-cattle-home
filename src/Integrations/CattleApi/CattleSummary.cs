namespace Defra.Lis.CattleHome.Integrations.CattleApi;

public sealed class CattleSummary
{
    public required string CattleId { get; init; }

    public required string Eartag { get; init; }

    public required string Breed { get; init; }

    public required string Sex { get; init; }
}
