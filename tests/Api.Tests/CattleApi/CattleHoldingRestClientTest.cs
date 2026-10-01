// <copyright file="CattleHoldingRestClientTest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.CattleApi;

using System.Net;
using Defra.Lis.Be4Fe.CattleApi;
using Defra.Lis.Be4Fe.Models.Lookups.Models;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Rest.Client;
using Defra.Livestock.Sdk.Api.Strategies.Operations;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Rest.Client;
using Microsoft.Extensions.Logging.Abstractions;

public sealed class CattleHoldingRestClientTest
{
    private const string OakfieldHolding = """
        {"cph":"22/001/0001","name":"Oakfield Farm","holdingType":"AH",
         "address":["Oakfield Farm","Church Lane","Shrewsbury","Shropshire","SY4 1AB","England"],
         "keeperName":"Oakfield Farmer","herdMarks":["UK 324537"],"allowedSpecies":["Cattle"]}
        """;

    private const string TwoCattle = """
        [
          {"earTag":"UK200000000001","dateBirth":"2023-02-01","dateOnCph":"2023-02-15","sex":"Male","breed":"Aberdeen Angus","breedCode":"AA","breedName":"Aberdeen Angus","status":"Alive","errors":[]},
          {"earTag":"UK200000000002","dateBirth":null,"dateOnCph":null,"sex":null,"breed":null,"breedCode":null,"breedName":null,"status":"Alive","errors":[]}
        ]
        """;

    private const string OakfieldAnimal = """
        {"earTag":"UK200000000001","species":"Cattle","sex":"Male","dateBirth":"2023-02-01",
         "dateRegistered":"2023-02-05","dateOnCph":"2023-02-01","breed":"Aberdeen Angus",
         "breedCode":"AA","breedName":"Aberdeen Angus","state":"Alive","restrictionStatus":"None",
         "damType":"genetic","geneticDamEarTag":"UK200000000098","surrogateDamEarTag":null,
         "sireEarTag":"UK200000000099","sireName":null}
        """;

    private const string TestUser = """
        {"subject":"0b6f2f0e-3c1a-4e8e-9d4b-2f6a1c9e7d51","email":"test.keeper@example.com",
         "firstName":"Test","lastName":"Keeper","displayName":"Test Keeper",
         "cphs":[{"cph":"22/001/0001","holdingId":"holding-0001","holdingName":"Oakfield Farm","role":"Keeper"},
                 {"cph":"22/003/0003","holdingId":null,"holdingName":null,"role":"Keeper"},
                 {"cph":null,"holdingId":null,"holdingName":null,"role":"Keeper"}]}
        """;

    private readonly StubHttpMessageHandler handler = new();

    [Fact]
    public async Task GetHoldingDetailsShouldCallTheCattleApiWithTheApiKey()
    {
        handler.RespondWith(HttpStatusCode.OK, OakfieldHolding);
        var client = CreateClient(apiKey: "test-key");

        await client.GetHoldingDetailsAsync("22/001/0001", TestContext.Current.CancellationToken);

        var request = handler.Requests.ShouldHaveSingleItem();
        request.Method.ShouldBe(HttpMethod.Get);
        request.RequestUri!.ToString().ShouldBe("http://cattle-api.test/v1/holdings/22/001/0001");
        request.Headers.GetValues(CattleHoldingRestClient.ApiKeyHeaderName).ShouldBe(["test-key"]);
    }

    [Fact]
    public async Task GetHoldingDetailsShouldOmitTheApiKeyHeaderWhenNotConfigured()
    {
        handler.RespondWith(HttpStatusCode.OK, OakfieldHolding);
        var client = CreateClient(apiKey: null);

        await client.GetHoldingDetailsAsync("22/001/0001", TestContext.Current.CancellationToken);

        handler.Requests.ShouldHaveSingleItem().Headers.Contains(CattleHoldingRestClient.ApiKeyHeaderName).ShouldBeFalse();
    }

    [Fact]
    public async Task GetHoldingDetailsShouldMapTheCattleApiResponse()
    {
        handler.RespondWith(HttpStatusCode.OK, OakfieldHolding);
        var client = CreateClient();

        var details = await client.GetHoldingDetailsAsync("22/001/0001", TestContext.Current.CancellationToken);

        details.Cph.ShouldBe("22/001/0001");
        details.Name.ShouldBe("Oakfield Farm");
        details.HoldingType.ShouldBe("AH");
        details.RegisteredKeeper.ShouldBe("Oakfield Farmer");
        details.Address.ShouldBe(["Oakfield Farm", "Church Lane", "Shrewsbury", "Shropshire", "SY4 1AB", "England"]);
        details.HerdMarks.ShouldBe(["UK 324537"]);
        details.AllowedSpecies.ShouldBe(["Cattle"]);
        details.BusinessName.ShouldBeNull();
    }

    [Fact]
    public async Task GetHoldingDetailsShouldThrowHoldingNotFoundOn404()
    {
        handler.RespondWith(HttpStatusCode.NotFound, """{"title":"Not Found","status":404}""");
        var client = CreateClient();

        var exception = await Should.ThrowAsync<HoldingNotFoundException>(
            () => client.GetHoldingDetailsAsync("22/050/0050", TestContext.Current.CancellationToken));

        exception.Cph.ShouldBe("22/050/0050");
    }

    [Fact]
    public async Task GetHoldingDetailsShouldThrowArgumentExceptionWhenTheCattleApiRejectsTheCph()
    {
        handler.RespondWith(HttpStatusCode.BadRequest, """{"title":"Bad Request","status":400}""");
        var client = CreateClient();

        var exception = await Should.ThrowAsync<ArgumentException>(
            () => client.GetHoldingDetailsAsync("2/1/1", TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("2/1/1");
    }

    [Fact]
    public async Task SearchCattleShouldThrowArgumentExceptionWhenTheCattleApiRejectsTheCph()
    {
        handler.RespondWith(HttpStatusCode.BadRequest, """{"title":"Bad Request","status":400}""");
        var client = CreateClient();

        await Should.ThrowAsync<ArgumentException>(
            () => client.SearchCattleAsync("2/1/1", new CattleSearchQuery(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetHoldingDetailsShouldPropagateOtherFailures()
    {
        handler.RespondWith(HttpStatusCode.Unauthorized, """{"title":"Unauthorized","status":401}""");
        var client = CreateClient();

        await Should.ThrowAsync<RestResponseException>(
            () => client.GetHoldingDetailsAsync("22/001/0001", TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("22/001")]
    [InlineData("22/001/0001/extra")]
    [InlineData(" ")]
    public async Task GetHoldingDetailsShouldRejectMalformedCphWithoutCallingTheApi(string cph)
    {
        var client = CreateClient();

        await Should.ThrowAsync<ArgumentException>(() => client.GetHoldingDetailsAsync(cph, TestContext.Current.CancellationToken));
        handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchCattleShouldCallTheCattleRouteWithoutFiltersByDefault()
    {
        handler.RespondWith(HttpStatusCode.OK, TwoCattle);
        var client = CreateClient();

        var cattle = await client.SearchCattleAsync("22-001-0001", new CattleSearchQuery(), TestContext.Current.CancellationToken);

        handler.Requests.ShouldHaveSingleItem().RequestUri!.ToString().ShouldBe("http://cattle-api.test/v1/holdings/22/001/0001/cattle");
        cattle.Count.ShouldBe(2);
    }

    [Fact]
    public async Task SearchCattleShouldPassSuppliedFiltersAsQueryParameters()
    {
        handler.RespondWith(HttpStatusCode.OK, "[]");
        var client = CreateClient();

        var cattle = await client.SearchCattleAsync("22/001/0001", new CattleSearchQuery(Eartag: " UK2000 ", Breed: "AA", Sex: "female"), TestContext.Current.CancellationToken);

        handler.Requests.ShouldHaveSingleItem().RequestUri!.Query.ShouldBe("?earTag=UK2000&breed=AA&sex=female");
        cattle.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchCattleShouldMapEachAnimalIncludingSparseRecords()
    {
        handler.RespondWith(HttpStatusCode.OK, TwoCattle);
        var client = CreateClient();

        var cattle = (await client.SearchCattleAsync("22/001/0001", new CattleSearchQuery(), TestContext.Current.CancellationToken)).ToList();

        cattle[0].CattleId.ShouldBe("UK200000000001");
        cattle[0].Eartag.ShouldBe("UK200000000001");
        cattle[0].Breed.ShouldBe("Aberdeen Angus");
        cattle[0].BreedCode.ShouldBe("AA");
        cattle[0].BreedName.ShouldBe("Aberdeen Angus");
        cattle[0].DateOfBirth.ShouldBe(new DateOnly(2023, 2, 1));
        cattle[0].DateOnCph.ShouldBe(new DateOnly(2023, 2, 15));
        cattle[0].Sex.ShouldBe("Male");
        cattle[0].Status.ShouldBe("Alive");

        cattle[1].Eartag.ShouldBe("UK200000000002");
        cattle[1].Breed.ShouldBe(string.Empty);
        cattle[1].DateOfBirth.ShouldBeNull();
        cattle[1].Sex.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task SearchCattleShouldThrowHoldingNotFoundOn404()
    {
        handler.RespondWith(HttpStatusCode.NotFound, """{"title":"Not Found","status":404}""");
        var client = CreateClient();

        await Should.ThrowAsync<HoldingNotFoundException>(
            () => client.SearchCattleAsync("22/050/0050", new CattleSearchQuery(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetCattleDetailsShouldCallTheCattleRouteWithTheApiKey()
    {
        handler.RespondWith(HttpStatusCode.OK, OakfieldAnimal);
        var client = CreateClient(apiKey: "test-key");

        await client.GetCattleDetailsAsync("UK200000000001", TestContext.Current.CancellationToken);

        var request = handler.Requests.ShouldHaveSingleItem();
        request.Method.ShouldBe(HttpMethod.Get);
        request.RequestUri!.ToString().ShouldBe("http://cattle-api.test/v1/cattle/UK200000000001");
        request.Headers.GetValues(CattleHoldingRestClient.ApiKeyHeaderName).ShouldBe(["test-key"]);
    }

    [Fact]
    public async Task GetCattleDetailsShouldMapEveryFieldOntoTheUiContract()
    {
        handler.RespondWith(HttpStatusCode.OK, OakfieldAnimal);
        var client = CreateClient();

        var details = await client.GetCattleDetailsAsync("UK200000000001", TestContext.Current.CancellationToken);

        details.CattleId.ShouldBe("UK200000000001");
        details.Eartag.ShouldBe("UK200000000001");
        details.Species.ShouldBe("Cattle");
        details.Sex.ShouldBe("Male");
        details.DateOfBirth.ShouldBe(new DateOnly(2023, 2, 1));
        details.DateRegistered.ShouldBe(new DateOnly(2023, 2, 5));
        details.DateOnCph.ShouldBe(new DateOnly(2023, 2, 1));

        // The UI resolves the breed name from the code, so Breed carries the code.
        details.Breed.ShouldBe("AA");
        details.BreedName.ShouldBe("Aberdeen Angus");
        details.State.ShouldBe("Alive");
        details.RestrictionStatus.ShouldBe("None");
        details.DamType.ShouldBe("genetic");
        details.GeneticDamTag.ShouldBe("UK200000000098");
        details.SurrogateTag.ShouldBeNull();
        details.SireTag.ShouldBe("UK200000000099");
        details.SireName.ShouldBeNull();
    }

    [Fact]
    public async Task GetCattleDetailsShouldLeaveCphUnsetBecauseCadsDoesNotReturnIt()
    {
        handler.RespondWith(HttpStatusCode.OK, OakfieldAnimal);
        var client = CreateClient();

        var details = await client.GetCattleDetailsAsync("UK200000000001", TestContext.Current.CancellationToken);

        details.Cph.ShouldBeNull();
    }

    [Fact]
    public async Task GetCattleDetailsShouldEscapeTheEarTagAndTrimIt()
    {
        handler.RespondWith(HttpStatusCode.OK, OakfieldAnimal);
        var client = CreateClient();

        await client.GetCattleDetailsAsync("  UK2 0000 00001  ", TestContext.Current.CancellationToken);

        // AbsoluteUri, not ToString(), which renders %20 back as a space.
        handler.Requests.ShouldHaveSingleItem().RequestUri!.AbsoluteUri
            .ShouldBe("http://cattle-api.test/v1/cattle/UK2%200000%2000001");
    }

    [Fact]
    public async Task GetCattleDetailsShouldThrowCattleNotFoundOn404()
    {
        handler.RespondWith(HttpStatusCode.NotFound, """{"title":"Not Found","status":404}""");
        var client = CreateClient();

        var exception = await Should.ThrowAsync<CattleNotFoundException>(
            () => client.GetCattleDetailsAsync("UK999999999999", TestContext.Current.CancellationToken));

        exception.EarTag.ShouldBe("UK999999999999");
    }

    [Fact]
    public async Task GetCattleDetailsShouldPropagateOtherFailures()
    {
        handler.RespondWith(HttpStatusCode.Unauthorized, """{"title":"Unauthorized","status":401}""");
        var client = CreateClient();

        await Should.ThrowAsync<RestResponseException>(
            () => client.GetCattleDetailsAsync("UK200000000001", TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task GetCattleDetailsShouldRejectABlankEarTagWithoutCallingTheApi(string earTag)
    {
        var client = CreateClient();

        await Should.ThrowAsync<ArgumentException>(() => client.GetCattleDetailsAsync(earTag, TestContext.Current.CancellationToken));
        handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetUserDetailsShouldCallTheCattleApiWithTheApiKey()
    {
        handler.RespondWith(HttpStatusCode.OK, TestUser);
        var client = CreateClient(apiKey: "test-key");

        await client.GetUserDetailsAsync("0b6f2f0e-3c1a-4e8e-9d4b-2f6a1c9e7d51", TestContext.Current.CancellationToken);

        var request = handler.Requests.ShouldHaveSingleItem();
        request.Method.ShouldBe(HttpMethod.Get);
        request.RequestUri!.AbsoluteUri.ShouldBe("http://cattle-api.test/v1/users/0b6f2f0e-3c1a-4e8e-9d4b-2f6a1c9e7d51");
        request.Headers.GetValues(CattleHoldingRestClient.ApiKeyHeaderName).ShouldBe(["test-key"]);
    }

    [Fact]
    public async Task GetUserDetailsShouldEscapeAndTrimTheUserIdWithoutChangingItsCase()
    {
        handler.RespondWith(HttpStatusCode.OK, TestUser);
        var client = CreateClient();

        await client.GetUserDetailsAsync("  Idp|User/1  ", TestContext.Current.CancellationToken);

        handler.Requests.ShouldHaveSingleItem().RequestUri!.AbsoluteUri
            .ShouldBe("http://cattle-api.test/v1/users/Idp%7CUser%2F1");
    }

    [Fact]
    public async Task GetUserDetailsShouldMapTheCattleApiResponse()
    {
        handler.RespondWith(HttpStatusCode.OK, TestUser);
        var client = CreateClient();

        var user = await client.GetUserDetailsAsync("0b6f2f0e-3c1a-4e8e-9d4b-2f6a1c9e7d51", TestContext.Current.CancellationToken);

        user.Subject.ShouldBe("0b6f2f0e-3c1a-4e8e-9d4b-2f6a1c9e7d51");
        user.Email.ShouldBe("test.keeper@example.com");
        user.FirstName.ShouldBe("Test");
        user.LastName.ShouldBe("Keeper");
        user.DisplayName.ShouldBe("Test Keeper");
        user.Cphs.Select(cph => cph.Cph).ShouldBe(["22/001/0001", "22/003/0003"]);
        var first = user.Cphs.First();
        first.HoldingId.ShouldBe("holding-0001");
        first.HoldingName.ShouldBe("Oakfield Farm");
        first.Role.ShouldBe("Keeper");
        user.Cphs.Last().HoldingName.ShouldBeNull();
    }

    [Fact]
    public async Task GetUserDetailsShouldThrowUserNotFoundOn404()
    {
        handler.RespondWith(HttpStatusCode.NotFound, """{"title":"Not Found","status":404}""");
        var client = CreateClient();

        var exception = await Should.ThrowAsync<UserNotFoundException>(
            () => client.GetUserDetailsAsync("00000000-0000-4000-8000-000000000000", TestContext.Current.CancellationToken));

        exception.UserId.ShouldBe("00000000-0000-4000-8000-000000000000");
    }

    [Fact]
    public async Task GetUserDetailsShouldThrowArgumentExceptionOn400()
    {
        handler.RespondWith(HttpStatusCode.BadRequest, """{"title":"Bad Request","status":400}""");
        var client = CreateClient();

        await Should.ThrowAsync<ArgumentException>(
            () => client.GetUserDetailsAsync("not-a-uuid", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetUserDetailsShouldPropagateOtherFailures()
    {
        handler.RespondWith(HttpStatusCode.InternalServerError, """{"title":"Internal Server Error","status":500}""");
        var client = CreateClient();

        await Should.ThrowAsync<RestResponseException>(
            () => client.GetUserDetailsAsync("0b6f2f0e-3c1a-4e8e-9d4b-2f6a1c9e7d51", TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task GetUserDetailsShouldRejectABlankUserIdWithoutCallingTheApi(string userId)
    {
        var client = CreateClient();

        await Should.ThrowAsync<ArgumentException>(() => client.GetUserDetailsAsync(userId, TestContext.Current.CancellationToken));
        handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task ClientShouldFailClearlyWhenTheBaseUrlIsNotConfigured()
    {
        var client = CreateClient(baseUrl: string.Empty);

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => client.GetHoldingDetailsAsync("22/001/0001", TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("CattleApi:BaseUrl");
        handler.Requests.ShouldBeEmpty();
    }

    private CattleHoldingRestClient CreateClient(string baseUrl = "http://cattle-api.test/", string? apiKey = "key")
    {
        var options = new CattleApiOptions { BaseUrl = baseUrl, ApiKey = apiKey };
        var factory = new RestStrategyFactory<CattleHoldingRestClient>(new StubServiceProvider(handler));

        return new CattleHoldingRestClient(factory, options, NullLogger<CattleHoldingRestClient>.Instance);
    }

    /// <summary>
    /// Supplies a fresh SDK REST client over the stub transport each time the factory builds a strategy.
    /// </summary>
    private sealed class StubServiceProvider(StubHttpMessageHandler handler) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            return serviceType == typeof(IRestHttpClient)
                ? new RestHttpClient(new HttpClient(handler), NullLogger<RestHttpClient>.Instance)
                : null;
        }
    }
}
