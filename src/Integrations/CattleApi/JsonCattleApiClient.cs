// <copyright file="JsonCattleApiClient.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi;

using System.Text.Json;
using System.Text.Json.Serialization;
using Defra.Lis.Be4Fe.Models.Lookups.Models;

public sealed class JsonCattleApiClient : ICattleApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IReadOnlyCollection<FixtureCattle> cattle;
    private readonly IReadOnlyCollection<UserFixture> users;

    public JsonCattleApiClient(string fixturePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fixturePath);

        if (!File.Exists(fixturePath))
        {
            throw new FileNotFoundException("The cattle API fixture file could not be found.", fixturePath);
        }

        using var stream = File.OpenRead(fixturePath);
        var fixture = JsonSerializer.Deserialize<CattleFixture>(stream, SerializerOptions)
            ?? throw new InvalidOperationException($"The cattle API fixture '{fixturePath}' is empty or invalid.");

        cattle = fixture.Cattle;
        users = fixture.Users;
    }

    public Task<IReadOnlyCollection<UserCph>> GetCphsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = users.FirstOrDefault(entry =>
            string.Equals(entry.UserId, userId.Trim(), StringComparison.OrdinalIgnoreCase));

        var result = user?.Holdings
            .SelectMany(holding => holding.Cphs.Select(cph => new UserCph
            {
                Cph = cph.Cph,
                Name = holding.GroupName,
                BusinessName = holding.BusinessName,
                Address = holding.Address,
                HoldingType = holding.HoldingType,
                RegisteredKeeper = holding.RegisteredKeeper,
                HerdMarks = holding.HerdMarks,
                AllowedSpecies = cph.AllowedSpecies,
                Postcode = cph.Postcode,
                Latitude = cph.Latitude,
                Longitude = cph.Longitude,
            }))
            .ToList()
            ?? [];

        return Task.FromResult<IReadOnlyCollection<UserCph>>(result);
    }

    public Task<CattleDetails> GetCattleDetailsAsync(
        string cattleId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalisedCattleId = NormaliseCattleId(cattleId);
        var fixtureEntry = cattle.FirstOrDefault(entry =>
            NormaliseCattleId(entry.Eartag) == normalisedCattleId);

        var result = fixtureEntry is null
            ? null
            : new CattleDetails
            {
                CattleId = fixtureEntry.Eartag,
                Eartag = fixtureEntry.Eartag,
                Cph = fixtureEntry.Cph,
                Breed = fixtureEntry.Breed,
                Sex = fixtureEntry.Sex,
                DateOfBirth = fixtureEntry.DateOfBirth,
                Status = fixtureEntry.Status,
                DamType = fixtureEntry.DamType,
                GeneticDamTag = fixtureEntry.GeneticDamTag,
                SurrogateTag = fixtureEntry.SurrogateTag,
                SireTag = fixtureEntry.SireTag,
                SireName = fixtureEntry.SireName,
            };

        return Task.FromResult(result ?? throw new KeyNotFoundException($"Cattle '{cattleId}' was not found in the fixture data."));
    }

    private static string NormaliseCattleId(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
    }

    private sealed record CattleFixture(
        IReadOnlyCollection<UserFixture> Users,
        IReadOnlyCollection<FixtureCattle> Cattle);

    private sealed record UserFixture(string UserId, IReadOnlyCollection<HoldingFixture> Holdings);

    private sealed record HoldingFixture(
        [property: JsonPropertyName("group_name")] string GroupName,
        [property: JsonPropertyName("business_name")] string BusinessName,
        IReadOnlyCollection<string> Address,
        [property: JsonPropertyName("holding_type")] string HoldingType,
        [property: JsonPropertyName("registered_keeper")] string RegisteredKeeper,
        [property: JsonPropertyName("herd_marks")] IReadOnlyCollection<string> HerdMarks,
        IReadOnlyCollection<CphFixture> Cphs);

    private sealed record CphFixture(
        string Cph,
        IReadOnlyCollection<string> AllowedSpecies,
        string Postcode,
        decimal Latitude,
        decimal Longitude);

    private sealed record FixtureCattle(
        string Eartag,
        string Cph,
        string Breed,
        [property: JsonPropertyName("dob")] DateOnly DateOfBirth,
        string Sex,
        [property: JsonPropertyName("dam_type")] string DamType,
        [property: JsonPropertyName("genetic_dam_tag")] string GeneticDamTag,
        [property: JsonPropertyName("surrogate_tag")] string SurrogateTag,
        [property: JsonPropertyName("sire_tag")] string SireTag,
        [property: JsonPropertyName("sire_name")] string SireName,
        string Status);
}
