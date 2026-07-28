// <copyright file="JsonCattleApiClientTest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi.Tests;

public sealed class JsonCattleApiClientTest
{
    private readonly JsonCattleApiClient client = new(
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "CattleApi", "cattle.json"));

    [Fact]
    public async Task GetCphsForUserShouldReturnMatchingFixtureEntries()
    {
        var result = await client.GetCphsForUserAsync(
            "FAIRFIELD.FARMER@FAIRFIELD-FARMS.CO.UK",
            TestContext.Current.CancellationToken);

        result.Count.ShouldBe(3);
        result.First().Name.ShouldBe("Fairfield Farm");
        result.First().BusinessName.ShouldBe("Fairfield Livestock Ltd");
        result.First().Address.ShouldBe([
            "Fairfield Farm",
            "Manor Road",
            "Lavendon",
            "Buckinghamshire",
            "MK1 1AA",
            "England",
        ]);
        result.First().HoldingType.ShouldBe("Permanent");
        result.First().RegisteredKeeper.ShouldBe("Bob McDougle");
        result.First().HerdMarks.ShouldBe(["UK 324787"]);
        result.First().AllowedSpecies.ShouldBe(["ctt"]);
        result.First().Postcode.ShouldBe("MK11 1EU");
        result.Last().Name.ShouldBe("Fairfield Farm");
    }

    [Fact]
    public async Task GetCattleForCphShouldReturnMatchingFixtureEntries()
    {
        var result = await client.GetCattleForCphAsync("10-081-1234", TestContext.Current.CancellationToken);

        result.Count.ShouldBe(10);
        result.First().CattleId.ShouldBe("UK123456100001");
        result.First().DateOfBirth.ShouldBe(new DateOnly(2024, 1, 15));
        result.First().Status.ShouldBe("saved");
    }

    [Fact]
    public async Task GetCattleDetailsShouldMatchCattleIdIgnoringCase()
    {
        var result = await client.GetCattleDetailsAsync("uk123456100019", TestContext.Current.CancellationToken);

        result.Eartag.ShouldBe("UK123456100019");
        result.Cph.ShouldBe("12/091/9576");
        result.DateOfBirth.ShouldBe(new DateOnly(2024, 10, 21));
        result.DamType.ShouldBe("surrogate");
        result.SireName.ShouldBe("Ivy");
    }

    [Fact]
    public void ConstructorShouldRejectAnEmptyPath()
    {
        Should.Throw<ArgumentException>(() => new JsonCattleApiClient(string.Empty));
    }

    [Fact]
    public void ConstructorShouldRejectAMissingFixture()
    {
        Should.Throw<FileNotFoundException>(() => new JsonCattleApiClient("missing-cattle-fixture.json"));
    }

    [Fact]
    public async Task GetCphsForUnknownUserShouldReturnAnEmptyCollection()
    {
        var result = await client.GetCphsForUserAsync("unknown@example.gov.uk", TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetCattleForUnknownCphShouldReturnAnEmptyCollection()
    {
        var result = await client.GetCattleForCphAsync("99/999/9999", TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetCattleDetailsShouldRejectAnUnknownId()
    {
        var exception = await Should.ThrowAsync<KeyNotFoundException>(
            () => client.GetCattleDetailsAsync("unknown", TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("unknown");
    }

    [Fact]
    public async Task OperationsShouldObserveCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            () => client.GetCphsForUserAsync("user", cancellation.Token));
        await Should.ThrowAsync<OperationCanceledException>(
            () => client.GetCattleForCphAsync("10/081/1234", cancellation.Token));
        await Should.ThrowAsync<OperationCanceledException>(
            () => client.GetCattleDetailsAsync("UK123456100001", cancellation.Token));
    }
}
