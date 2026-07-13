namespace Defra.Lis.CattleHome.Integrations.CattleApi;

public sealed class FakeCattleApiClient : ICattleApiClient
{
    public Task<IReadOnlyCollection<CattleSummary>> GetCattleForCphAsync(string cph, CancellationToken cancellationToken = default)
    {
        var normalisedCph = cph.Trim().ToUpperInvariant();
        var seed = DemoDataSeed.From(normalisedCph);
        var cattle = Enumerable.Range(1, 3)
            .Select(index => new CattleSummary
            {
                CattleId = $"{ExtractDigits(normalisedCph)}-{index:000}",
                Eartag = $"UK{100000000 + seed + index}",
                Breed = index % 2 == 0 ? "Limousin" : "Aberdeen Angus",
                Sex = index % 2 == 0 ? "Female" : "Male",
            })
            .ToList();

        return Task.FromResult<IReadOnlyCollection<CattleSummary>>(cattle);
    }

    public Task<CattleDetails> GetCattleDetailsAsync(string cattleId, CancellationToken cancellationToken = default)
    {
        var normalisedCattleId = cattleId.Trim().ToUpperInvariant();
        var seed = DemoDataSeed.From(normalisedCattleId);

        var details = new CattleDetails
        {
            CattleId = normalisedCattleId,
            Eartag = $"UK{200000000 + seed}",
            Cph = $"56/{100 + (seed % 900):000}/{1000 + (seed % 9000):0000}",
            Breed = seed % 2 == 0 ? "Hereford" : "Holstein Friesian",
            Sex = seed % 2 == 0 ? "Female" : "Male",
            DateOfBirth = new DateOnly(2020 + (seed % 4), 1 + (seed % 12), 1 + (seed % 27)),
            Status = seed % 2 == 0 ? "Active" : "In transit",
        };

        return Task.FromResult(details);
    }

    private static string ExtractDigits(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? "000000000" : digits;
    }
}
