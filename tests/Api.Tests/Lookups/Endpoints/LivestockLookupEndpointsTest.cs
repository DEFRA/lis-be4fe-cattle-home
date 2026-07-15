// <copyright file="LivestockLookupEndpointsTest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.Lookups.Endpoints;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Defra.Lis.Be4Fe.Api;
using Defra.Lis.Be4Fe.Api.Foundation.Caching;
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
    public async Task GetCphsForUserShouldCacheProviderResponse()
    {
        await using var factory = new LookupTestFactory();
        var client = factory.CreateClient();

        var first = await client.GetFromJsonAsync<CachedLookupResponse<List<UserCph>>>(
            "/api/users/alice/cphs",
            SerializerOptions,
            TestContext.Current.CancellationToken);
        var second = await client.GetFromJsonAsync<CachedLookupResponse<List<UserCph>>>(
            "/api/users/alice/cphs",
            SerializerOptions,
            TestContext.Current.CancellationToken);

        first.ShouldNotBeNull();
        second.ShouldNotBeNull();
        first.Source.ShouldBe("cph-provider");
        second.Source.ShouldBe("cache");
        first.Data.Count.ShouldBe(2);
        second.Data.Count.ShouldBe(2);
        factory.CattleApiClient.UserCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetCattleForCphShouldReturnCattleFromProvider()
    {
        await using var factory = new LookupTestFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/cphs/12/345/6789/cattle", TestContext.Current.CancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<CachedLookupResponse<List<CattleSummary>>>(
            SerializerOptions,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payload.ShouldNotBeNull();
        payload.Source.ShouldBe("cattle");
        payload.Data.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetCattleDetailsShouldReturnDetailsFromProvider()
    {
        await using var factory = new LookupTestFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/cattle/animal-123", TestContext.Current.CancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<CachedLookupResponse<CattleDetails>>(
            SerializerOptions,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payload.ShouldNotBeNull();
        payload.Source.ShouldBe("cattle");
        payload.Data.CattleId.ShouldBe("ANIMAL-123");
    }

    private sealed class LookupTestFactory : WebApplicationFactory<Program>
    {
        public TestCattleApiClient CattleApiClient { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IExternalDataCacheRepository>();
                services.RemoveAll<ICattleApiClient>();

                services.AddSingleton<IExternalDataCacheRepository, InMemoryExternalDataCacheRepository>();
                services.AddSingleton<ICattleApiClient>(CattleApiClient);
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

    private sealed class TestCattleApiClient : ICattleApiClient
    {
        public int UserCallCount { get; private set; }

        public Task<IReadOnlyCollection<UserCph>> GetCphsForUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            UserCallCount++;

            return Task.FromResult<IReadOnlyCollection<UserCph>>(
            [
                new UserCph { Cph = "12/345/6789", Name = $"{userId} Primary" },
                new UserCph { Cph = "98/765/4321", Name = $"{userId} Secondary" },
            ]);
        }

        public Task<IReadOnlyCollection<CattleSummary>> GetCattleForCphAsync(string cph, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<CattleSummary>>(
            [
                new CattleSummary { CattleId = $"{cph}-001", Eartag = "UK123", Breed = "Angus", Sex = "Female" },
                new CattleSummary { CattleId = $"{cph}-002", Eartag = "UK124", Breed = "Hereford", Sex = "Male" },
            ]);
        }

        public Task<CattleDetails> GetCattleDetailsAsync(string cattleId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CattleDetails
            {
                CattleId = cattleId,
                Eartag = "UK999",
                Cph = "12/345/6789",
                Breed = "Angus",
                Sex = "Female",
                DateOfBirth = new DateOnly(2021, 2, 3),
                Status = "Active",
            });
        }
    }
}
