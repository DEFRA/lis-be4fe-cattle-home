namespace Defra.Lis.CattleHome.Foundation.Models;

public sealed class DependencyConfiguration
{
    public required DependencyStatus Mongo { get; init; }

    public required DependencyStatus CattleApi { get; init; }
}
