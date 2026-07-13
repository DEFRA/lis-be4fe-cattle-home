namespace Defra.Lis.CattleHome.Integrations.CattleApi;

internal static class DemoDataSeed
{
    public static int From(string input)
    {
        var seed = 17;

        foreach (var character in input.ToUpperInvariant())
        {
            seed = (seed * 31) + character;
        }

        return Math.Abs(seed);
    }
}
