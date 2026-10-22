// <copyright file="LivestockLookupEndpointsTest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.Lookups.Endpoints;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Defra.Lis.Be4Fe.Api;
using Defra.Lis.Be4Fe.Api.Authentication;
using Defra.Lis.Be4Fe.Api.Foundation.Caching;
using Defra.Lis.Be4Fe.Api.Middleware.Headers;
using Defra.Lis.Be4Fe.CattleApi;
using Defra.Lis.Be4Fe.Models.Lookups.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public class LivestockLookupEndpointsTest
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    [Fact]
    public async Task GetCphsForUserShouldReturnTheUsersCphsFromTheCattleApiWithoutCaching()
    {
        await using var factory = new LookupTestFactory();
        var client = factory.CreateAuthenticatedClient();

        await client.GetAsync("/api/users/0b6f2f0e-3c1a-4e8e-9d4b-2f6a1c9e7d51/cphs", TestContext.Current.CancellationToken);
        var response = await client.GetAsync("/api/users/0b6f2f0e-3c1a-4e8e-9d4b-2f6a1c9e7d51/cphs", TestContext.Current.CancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<CachedLookupResponse<List<UserHolding>>>(
            SerializerOptions,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payload.ShouldNotBeNull();
        payload.Source.ShouldBe("cattle-api");
        payload.CachedUntilUtc.ShouldBeNull();
        var cph = payload.Data.ShouldHaveSingleItem();
        cph.Cph.ShouldBe("22/001/0001");
        cph.HoldingId.ShouldBe("holding-0001");
        cph.HoldingName.ShouldBe("Oakfield Farm");
        cph.Role.ShouldBe("Keeper");
        factory.HoldingClient.UserDetailCallCount.ShouldBe(2);
    }

    [Fact]
    public async Task GetCphsForUserShouldPassTheUserIdThroughWithoutChangingItsCase()
    {
        await using var factory = new LookupTestFactory();
        var client = factory.CreateAuthenticatedClient();

        await client.GetAsync("/api/users/AbC-def/cphs", TestContext.Current.CancellationToken);

        factory.HoldingClient.LastUserId.ShouldBe("AbC-def");
    }

    [Theory]
    [InlineData("unknown-user", HttpStatusCode.NotFound)]
    [InlineData("rejected-user", HttpStatusCode.BadRequest)]
    [InlineData("unavailable-user", HttpStatusCode.ServiceUnavailable)]
    public async Task GetCphsForUserShouldReturnProblemDetailsForCattleApiFailures(string userId, HttpStatusCode expected)
    {
        await using var factory = new LookupTestFactory();
        var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/users/{userId}/cphs", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(expected);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain($"\"status\":{(int)expected}");
    }

    [Fact]
    public async Task GetCattleForCphShouldReturnCattleFromProvider()
    {
        await using var factory = new LookupTestFactory();
        var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/cphs/12/345/6789/cattle", TestContext.Current.CancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<CachedLookupResponse<List<CattleSummary>>>(
            SerializerOptions,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payload.ShouldNotBeNull();
        payload.Source.ShouldBe("cattle-api");
        payload.CachedUntilUtc.ShouldBeNull();
        payload.Data.Count.ShouldBe(2);
        payload.Data[0].BreedCode.ShouldBe("AA");
        payload.Data[0].DateOnCph.ShouldBe(new DateOnly(2024, 2, 1));
        factory.HoldingClient.LastCph.ShouldBe("12/345/6789");
        factory.HoldingClient.LastQuery.ShouldNotBeNull().IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public async Task GetCattleForCphShouldPassSearchFiltersToTheCattleApi()
    {
        await using var factory = new LookupTestFactory();
        var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/cphs/12/345/6789/cattle?eartag=UK123&breed=Angus&sex=Female", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var query = factory.HoldingClient.LastQuery.ShouldNotBeNull();
        query.Eartag.ShouldBe("UK123");
        query.Breed.ShouldBe("Angus");
        query.Sex.ShouldBe("Female");
    }

    [Fact]
    public async Task GetHoldingDetailsShouldReturnDetailsFromTheCattleApi()
    {
        await using var factory = new LookupTestFactory();
        var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/cphs/12/345/6789", TestContext.Current.CancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<CachedLookupResponse<HoldingDetailedResponse>>(
            SerializerOptions,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payload.ShouldNotBeNull();
        payload.Source.ShouldBe("cattle-api");
        payload.Data.Cph.ShouldBe("12/345/6789");
        payload.Data.Name.ShouldBe("Test Farm");
        payload.Data.KeeperName.ShouldBe("Test Keeper");
        payload.Data.StartDate.ShouldBe(DateTimeOffset.Parse("2020-01-01T00:00:00+00:00"));
        payload.Data.EndDate.ShouldBeNull();
        payload.Data.Active.ShouldBe(true);
        var mark = payload.Data.HerdMarks.ShouldHaveSingleItem();
        mark.Mark.ShouldBe("UK 123456");
        mark.Species.ShouldBe(["Cattle"]);
        var assoc = payload.Data.Associations.ShouldHaveSingleItem();
        assoc.CustomerNumber.ShouldBe("CN123");
        assoc.Name.ShouldBe("Test Keeper");
        assoc.Roles.ShouldHaveSingleItem().Code.ShouldBe("KEEPER");
    }

    [Fact]
    public async Task GetHoldingDetailsShouldReturnProblemDetailsWhenTheHoldingIsUnknown()
    {
        await using var factory = new LookupTestFactory();
        var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/cphs/99/999/9999", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("99/999/9999");
    }

    [Fact]
    public async Task GetCattleDetailsShouldReturnDetailsFromTheCattleApi()
    {
        await using var factory = new LookupTestFactory();
        var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/cattle/uk200000000001", TestContext.Current.CancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<CachedLookupResponse<CattleDetails>>(
            SerializerOptions,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payload.ShouldNotBeNull();
        payload.Source.ShouldBe("cattle-api");
        payload.Data.CattleId.ShouldBe("UK200000000001");
        payload.Data.Breed.ShouldBe("AA");
        payload.Data.State.ShouldBe("Alive");
        payload.Data.RestrictionStatus.ShouldBe("None");
        payload.Data.DateRegistered.ShouldBe(new DateOnly(2021, 2, 7));
        payload.Data.DamType.ShouldBe("genetic");
        payload.Data.GeneticDamTag.ShouldBe("UK200000000098");
        payload.Data.Cph.ShouldBeNull();
        payload.Data.CurrentLocation.ShouldNotBeNull();
        payload.Data.CurrentLocation.Identifier.ShouldBe("22/001/0001");
        payload.Data.CurrentLocation.Name.ShouldBe("Oakfield Farm");
        payload.Data.RegisteredLocation.ShouldNotBeNull();
        payload.Data.RegisteredLocation.Identifier.ShouldBe("22/002/0002");
        payload.Data.RegisteredLocation.Name.ShouldBe("Birth Farm");
        payload.Data.DeathLocation.ShouldBeNull();
        factory.HoldingClient.LastEarTag.ShouldBe("UK200000000001");
    }

    [Fact]
    public async Task GetCattleDetailsShouldWriteTheResponseInSnakeCase()
    {
        await using var factory = new LookupTestFactory();
        var client = factory.CreateAuthenticatedClient();

        var body = await client.GetStringAsync("/api/cattle/uk200000000001", TestContext.Current.CancellationToken);

        body.ShouldContain("\"cattle_id\":\"uk200000000001\"");
        body.ShouldContain("\"current_location\":{");
        body.ShouldContain("\"full_identifier\":\"22/001/0001/sub1\"");
        body.ShouldContain("\"premises_type\":\"Agricultural\"");
    }

    [Fact]
    public async Task GetCattleDetailsShouldServeTheSecondCallFromTheCache()
    {
        await using var factory = new LookupTestFactory();
        var client = factory.CreateAuthenticatedClient();

        await client.GetAsync("/api/cattle/UK200000000001", TestContext.Current.CancellationToken);
        var second = await client.GetFromJsonAsync<CachedLookupResponse<CattleDetails>>(
            "/api/cattle/UK200000000001",
            SerializerOptions,
            TestContext.Current.CancellationToken);

        second.ShouldNotBeNull();
        second.Source.ShouldBe("cache");
        factory.HoldingClient.CattleDetailCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetCattleDetailsShouldReturnProblemDetails404WhenTheAnimalIsUnknown()
    {
        await using var factory = new LookupTestFactory();
        var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/cattle/UNKNOWN-TAG", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("UNKNOWN-TAG");
    }

    [Fact]
    public async Task GetUserDetailsShouldReturnDetailsFromTheCattleApiWithoutCaching()
    {
        await using var factory = new LookupTestFactory();
        var client = factory.CreateAuthenticatedClient();

        await client.GetAsync("/api/users/0b6f2f0e-3c1a-4e8e-9d4b-2f6a1c9e7d51", TestContext.Current.CancellationToken);
        var response = await client.GetAsync("/api/users/0b6f2f0e-3c1a-4e8e-9d4b-2f6a1c9e7d51", TestContext.Current.CancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<CachedLookupResponse<UserDetails>>(
            SerializerOptions,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payload.ShouldNotBeNull();
        payload.Source.ShouldBe("cattle-api");
        payload.CachedUntilUtc.ShouldBeNull();
        payload.Data.Subject.ShouldBe("0b6f2f0e-3c1a-4e8e-9d4b-2f6a1c9e7d51");
        payload.Data.DisplayName.ShouldBe("Test Keeper");
        var cph = payload.Data.Cphs.ShouldHaveSingleItem();
        cph.Cph.ShouldBe("22/001/0001");
        cph.HoldingId.ShouldBe("holding-0001");
        cph.HoldingName.ShouldBe("Oakfield Farm");
        cph.Role.ShouldBe("Keeper");
        factory.HoldingClient.UserDetailCallCount.ShouldBe(2);
    }

    [Fact]
    public async Task GetUserDetailsShouldPassTheUserIdThroughWithoutChangingItsCase()
    {
        await using var factory = new LookupTestFactory();
        var client = factory.CreateAuthenticatedClient();

        await client.GetAsync("/api/users/AbC-def", TestContext.Current.CancellationToken);

        factory.HoldingClient.LastUserId.ShouldBe("AbC-def");
    }

    [Fact]
    public async Task GetUserDetailsShouldWriteTheResponseInSnakeCase()
    {
        await using var factory = new LookupTestFactory();
        var client = factory.CreateAuthenticatedClient();

        var body = await client.GetStringAsync("/api/users/0b6f2f0e-3c1a-4e8e-9d4b-2f6a1c9e7d51", TestContext.Current.CancellationToken);

        body.ShouldContain("\"display_name\":\"Test Keeper\"");
        body.ShouldContain("\"holding_name\":\"Oakfield Farm\"");
    }

    [Fact]
    public async Task GetUserDetailsShouldReturnProblemDetails404WhenTheUserIsUnknown()
    {
        await using var factory = new LookupTestFactory();
        var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/users/unknown-user", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("unknown-user");
    }

    [Fact]
    public async Task GetUserDetailsShouldReturnProblemDetails400WhenTheCattleApiRejectsTheUserId()
    {
        await using var factory = new LookupTestFactory();
        var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/users/rejected-user", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateUserShouldPassTheClaimsToTheCattleApiAndWrapTheResponse()
    {
        await using var factory = new LookupTestFactory();
        var client = factory.CreateAuthenticatedClient();

        var response = await client.PostAsync("/api/users", ClaimsBody("0b6f2f0e-3c1a-4e8e-9d4b-2f6a1c9e7d51"), TestContext.Current.CancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<CachedLookupResponse<UserDetails>>(
            SerializerOptions,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payload.ShouldNotBeNull();
        payload.Source.ShouldBe("cattle-api");
        payload.CachedUntilUtc.ShouldBeNull();
        payload.Data.Subject.ShouldBe("0b6f2f0e-3c1a-4e8e-9d4b-2f6a1c9e7d51");
        payload.Data.Cphs.ShouldHaveSingleItem().Cph.ShouldBe("22/001/0001");
        var claims = factory.HoldingClient.LastCreateUserRequest.ShouldNotBeNull();
        claims.Sub.ShouldBe("0b6f2f0e-3c1a-4e8e-9d4b-2f6a1c9e7d51");
        claims.Email.ShouldBe("contact@test.com");
        claims.GivenName.ShouldBe("FirstName");
        claims.FamilyName.ShouldBe("LastName");
    }

    [Fact]
    public async Task CreateUserShouldWriteTheResponseInSnakeCase()
    {
        await using var factory = new LookupTestFactory();
        var client = factory.CreateAuthenticatedClient();

        var response = await client.PostAsync("/api/users", ClaimsBody("0b6f2f0e-3c1a-4e8e-9d4b-2f6a1c9e7d51"), TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        body.ShouldContain("\"cached_until_utc\":null");
        body.ShouldContain("\"display_name\":\"Test Keeper\"");
    }

    [Theory]
    [InlineData("rejected-user", HttpStatusCode.BadRequest)]
    [InlineData("conflict-user", HttpStatusCode.Conflict)]
    [InlineData("unavailable-user", HttpStatusCode.ServiceUnavailable)]
    public async Task CreateUserShouldReturnProblemDetailsForCattleApiFailures(string sub, HttpStatusCode expected)
    {
        await using var factory = new LookupTestFactory();
        var client = factory.CreateAuthenticatedClient();

        var response = await client.PostAsync("/api/users", ClaimsBody(sub), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(expected);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain($"\"status\":{(int)expected}");
    }

    [Fact]
    public async Task CreateUserShouldReturnProblemDetails400WhenTheBodyIsJsonNull()
    {
        await using var factory = new LookupTestFactory();
        var client = factory.CreateAuthenticatedClient();

        var response = await client.PostAsync(
            "/api/users",
            new StringContent("null", System.Text.Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        factory.HoldingClient.LastCreateUserRequest.ShouldBeNull();
    }

    [Fact]
    public async Task SearchHoldingsShouldReturnPaginatedHoldingsWithSearchPagingAndSortingParameters()
    {
        await using var factory = new LookupTestFactory();
        var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/cphs?search=oakfield&page=2&pageSize=20&sort=name&order=desc", TestContext.Current.CancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<PaginatedResponse<HoldingResponse>>(
            SerializerOptions,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payload.ShouldNotBeNull();
        payload.Count.ShouldBe(1);
        payload.TotalCount.ShouldBe(1);
        payload.Page.ShouldBe(2);
        payload.PageSize.ShouldBe(20);
        var holding = payload.Items.ShouldHaveSingleItem();
        holding.Cph.ShouldBe("22/001/0001");
        holding.Name.ShouldBe("Oakfield Farm");
        holding.HoldingType.ShouldBe("AH");
        holding.KeeperName.ShouldBe("Oakfield Farmer");
        holding.StartDate.ShouldBe(DateTimeOffset.Parse("2020-01-01T00:00:00+00:00"));
        holding.EndDate.ShouldBeNull();
        holding.Active.ShouldBe(true);

        factory.HoldingClient.LastHoldingQuery.ShouldNotBeNull();
        factory.HoldingClient.LastHoldingQuery.Search.ShouldBe("oakfield");
        factory.HoldingClient.LastHoldingQuery.Page.ShouldBe(2);
        factory.HoldingClient.LastHoldingQuery.PageSize.ShouldBe(20);
        factory.HoldingClient.LastHoldingQuery.Sort.ShouldBe("name");
        factory.HoldingClient.LastHoldingQuery.Order.ShouldBe("desc");
    }

    [Fact]
    public async Task SearchHoldingsShouldWriteResponseInSnakeCase()
    {
        await using var factory = new LookupTestFactory();
        var client = factory.CreateAuthenticatedClient();

        var body = await client.GetStringAsync("/api/cphs?search=oakfield", TestContext.Current.CancellationToken);

        body.ShouldContain("\"total_count\":1");
        body.ShouldContain("\"has_next_page\":false");
        body.ShouldContain("\"holding_type\":\"AH\"");
        body.ShouldContain("\"keeper_name\":\"Oakfield Farmer\"");
        body.ShouldContain("\"active\":true");
    }

    [Fact]
    public async Task SearchHoldingsShouldReturnProblemDetails400WhenRejectedByCattleApi()
    {
        await using var factory = new LookupTestFactory();
        var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/cphs?search=invalid", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private static StringContent ClaimsBody(string sub) => new(
        $$"""{"sub":"{{sub}}","email":"contact@test.com","given_name":"FirstName","family_name":"LastName"}""",
        System.Text.Encoding.UTF8,
        "application/json");

    private sealed class LookupTestFactory : WebApplicationFactory<Program>
    {
        private const string TestApiKey = "test-api-key";

        public TestCattleHoldingClient HoldingClient { get; } = new();

        public HttpClient CreateAuthenticatedClient()
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Add(RequestHeaderNames.ApiKey, TestApiKey);
            client.DefaultRequestHeaders.Add(RequestHeaderNames.CorrelationId, "corr-lookup-test");
            return client;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting($"{ApiKeyAuthenticationOptions.SectionName}:Keys:0", TestApiKey);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IExternalDataCacheRepository>();
                services.RemoveAll<ICattleHoldingClient>();

                services.AddSingleton<IExternalDataCacheRepository, InMemoryExternalDataCacheRepository>();
                services.AddSingleton<ICattleHoldingClient>(HoldingClient);
            });
        }
    }

    private sealed class TestCattleHoldingClient : ICattleHoldingClient
    {
        public string? LastCph { get; private set; }

        public string? LastEarTag { get; private set; }

        public int CattleDetailCallCount { get; private set; }

        public CattleSearchQuery? LastQuery { get; private set; }

        public HoldingSearchQuery? LastHoldingQuery { get; private set; }

        public string? LastUserId { get; private set; }

        public int UserDetailCallCount { get; private set; }

        public CreateUserRequest? LastCreateUserRequest { get; private set; }

        public Task<UserDetails> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
        {
            LastCreateUserRequest = request;
            var sub = request.Sub ?? string.Empty;

            if (sub.StartsWith("rejected", StringComparison.Ordinal))
            {
                throw new ArgumentException("The cattle API rejected the user account claims.", nameof(request));
            }

            if (sub.StartsWith("conflict", StringComparison.Ordinal))
            {
                throw new UserConflictException(sub);
            }

            if (sub.StartsWith("unavailable", StringComparison.Ordinal))
            {
                throw new ServiceUnavailableException("The user account service is temporarily unavailable, try again later.", new HttpRequestException());
            }

            return GetUserDetailsAsync(sub, cancellationToken);
        }

        public Task<PaginatedResponse<HoldingResponse>> SearchHoldingsAsync(HoldingSearchQuery query, CancellationToken cancellationToken = default)
        {
            LastHoldingQuery = query;

            if (query.Search == "invalid")
            {
                throw new ArgumentException("The cattle API rejected the holdings search query.", nameof(query));
            }

            return Task.FromResult(new PaginatedResponse<HoldingResponse>
            {
                Count = 1,
                TotalCount = 1,
                Page = query.Page ?? 1,
                PageSize = query.PageSize ?? 10,
                TotalPages = 1,
                HasNextPage = false,
                HasPreviousPage = false,
                Items =
                [
                    new HoldingResponse
                    {
                        Cph = "22/001/0001",
                        Name = "Oakfield Farm",
                        HoldingType = "AH",
                        Address = ["1 Farm Lane", "Shrewsbury", "SY1 1AA"],
                        KeeperName = "Oakfield Farmer",
                        StartDate = DateTimeOffset.Parse("2020-01-01T00:00:00+00:00"),
                        EndDate = null,
                        Active = true,
                    },
                ],
            });
        }

        public Task<UserDetails> GetUserDetailsAsync(string userId, CancellationToken cancellationToken = default)
        {
            LastUserId = userId;
            UserDetailCallCount++;

            if (userId.StartsWith("unknown", StringComparison.Ordinal))
            {
                throw new UserNotFoundException(userId);
            }

            if (userId.StartsWith("rejected", StringComparison.Ordinal))
            {
                throw new ArgumentException($"The cattle API rejected user '{userId}'.", nameof(userId));
            }

            if (userId.StartsWith("unavailable", StringComparison.Ordinal))
            {
                throw new ServiceUnavailableException("The user account service is temporarily unavailable, try again later.", new HttpRequestException());
            }

            return Task.FromResult(new UserDetails
            {
                Subject = userId,
                Email = "test.keeper@example.com",
                FirstName = "Test",
                LastName = "Keeper",
                DisplayName = "Test Keeper",
                Cphs = [new UserHolding { Cph = "22/001/0001", HoldingId = "holding-0001", HoldingName = "Oakfield Farm", Role = "Keeper" }],
            });
        }

        public Task<HoldingDetailedResponse> GetHoldingDetailsAsync(string cph, CancellationToken cancellationToken = default)
        {
            LastCph = cph;

            if (cph.StartsWith("99/", StringComparison.Ordinal))
            {
                throw new HoldingNotFoundException(cph);
            }

            return Task.FromResult(new HoldingDetailedResponse
            {
                Cph = cph,
                Name = "Test Farm",
                HoldingType = "AH",
                Address = ["1 Farm Lane", "Shrewsbury", "SY1 1AA"],
                KeeperName = "Test Keeper",
                StartDate = DateTimeOffset.Parse("2020-01-01T00:00:00+00:00"),
                EndDate = null,
                Active = true,
                HerdMarks = [new HoldingMarkResponse { Mark = "UK 123456", Species = ["Cattle"] }],
                AllowedSpecies = ["Cattle"],
                Associations =
                [
                    new HoldingAssociationResponse
                    {
                        CustomerNumber = "CN123",
                        Name = "Test Keeper",
                        Roles = [new HoldingRoleResponse { Code = "KEEPER", Species = ["Cattle"] }],
                    },
                ],
            });
        }

        public Task<IReadOnlyCollection<CattleSummary>> SearchCattleAsync(string cph, CattleSearchQuery query, CancellationToken cancellationToken = default)
        {
            LastCph = cph;
            LastQuery = query;

            return Task.FromResult<IReadOnlyCollection<CattleSummary>>(
            [
                new CattleSummary { CattleId = "UK123", Eartag = "UK123", Breed = "Aberdeen Angus", BreedCode = "AA", BreedName = "Aberdeen Angus", DateOfBirth = new DateOnly(2024, 1, 15), DateOnCph = new DateOnly(2024, 2, 1), Sex = "Female", Status = "Alive" },
                new CattleSummary { CattleId = "UK124", Eartag = "UK124", Breed = "Hereford", BreedCode = "HE", BreedName = "Hereford", DateOfBirth = new DateOnly(2024, 2, 3), Sex = "Male", Status = "Alive" },
            ]);
        }

        public Task<CattleDetails> GetCattleDetailsAsync(string earTag, CancellationToken cancellationToken = default)
        {
            LastEarTag = earTag;
            CattleDetailCallCount++;

            if (earTag.StartsWith("UNKNOWN", StringComparison.Ordinal))
            {
                throw new CattleNotFoundException(earTag);
            }

            return Task.FromResult(new CattleDetails
            {
                CattleId = earTag,
                Eartag = earTag,
                Cph = null,
                Breed = "AA",
                BreedName = "Aberdeen Angus",
                Species = "Cattle",
                Sex = "Female",
                DateOfBirth = new DateOnly(2021, 2, 3),
                DateRegistered = new DateOnly(2021, 2, 7),
                DateOnCph = new DateOnly(2021, 2, 3),
                CurrentLocation = new CattleLocation
                {
                    Identifier = "22/001/0001",
                    SubIdentifier = "sub1",
                    FullIdentifier = "22/001/0001/sub1",
                    Name = "Oakfield Farm",
                    PremisesType = "Agricultural",
                },
                RegisteredLocation = new CattleLocation
                {
                    Identifier = "22/002/0002",
                    SubIdentifier = null,
                    FullIdentifier = "22/002/0002",
                    Name = "Birth Farm",
                    PremisesType = "Agricultural",
                },
                DeathLocation = null,
                State = "Alive",
                RestrictionStatus = "None",
                DamType = "genetic",
                GeneticDamTag = "UK200000000098",
                SireTag = "UK200000000099",
            });
        }
    }

    private sealed class InMemoryExternalDataCacheRepository : IExternalDataCacheRepository
    {
        private readonly Dictionary<string, (string Json, DateTimeOffset ExpiresAtUtc)> cache = [];

        public Task<CacheLookupResult<T>?> GetAsync<T>(
            string cacheType,
            string cacheKey,
            CancellationToken cancellationToken = default)
        {
            var id = $"{cacheType}:{cacheKey}";

            if (!cache.TryGetValue(id, out var entry) || entry.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            {
                return Task.FromResult<CacheLookupResult<T>?>(null);
            }

            var value = System.Text.Json.JsonSerializer.Deserialize<T>(entry.Json, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

            return Task.FromResult<CacheLookupResult<T>?>(new CacheLookupResult<T>
            {
                Value = value!,
                ExpiresAtUtc = entry.ExpiresAtUtc,
            });
        }

        public Task SetAsync<T>(
            string cacheType,
            string cacheKey,
            T value,
            DateTimeOffset expiresAtUtc,
            CancellationToken cancellationToken = default)
        {
            var id = $"{cacheType}:{cacheKey}";
            var json = System.Text.Json.JsonSerializer.Serialize(value, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
            cache[id] = (json, expiresAtUtc);
            return Task.CompletedTask;
        }
    }
}
