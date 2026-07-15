// <copyright file="CachedDataServiceTest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.Services;

using Defra.Lis.Be4Fe.Api.Foundation.Caching;
using Defra.Lis.Be4Fe.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

public sealed class CachedDataServiceTest
{
    [Fact]
    public async Task StoreAndWrapShouldNotCacheAnEmptyCollection()
    {
        var repository = new RecordingCacheRepository();
        var service = CreateService(repository);

        var response = await service.StoreAndWrapAsync(
            "cattle",
            "unknown-cph",
            new List<string>(),
            "provider",
            TestContext.Current.CancellationToken);

        repository.WriteCount.ShouldBe(0);
        response.CachedUntilUtc.ShouldBeNull();
        response.Data.ShouldBeEmpty();
    }

    [Fact]
    public async Task StoreAndWrapShouldCacheAResultContainingData()
    {
        var repository = new RecordingCacheRepository();
        var service = CreateService(repository);

        var response = await service.StoreAndWrapAsync(
            "cattle",
            "known-cph",
            new List<string> { "animal" },
            "provider",
            TestContext.Current.CancellationToken);

        repository.WriteCount.ShouldBe(1);
        response.CachedUntilUtc.ShouldNotBeNull();
    }

    private static CachedDataService CreateService(IExternalDataCacheRepository repository)
    {
        return new CachedDataService(
            repository,
            Options.Create(new CacheOptions { DurationMinutes = 5 }),
            TimeProvider.System,
            NullLogger<CachedDataService>.Instance);
    }

    private sealed class RecordingCacheRepository : IExternalDataCacheRepository
    {
        public int WriteCount { get; private set; }

        public Task<CacheLookupResult<T>?> GetAsync<T>(
            string cacheType,
            string cacheKey,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<CacheLookupResult<T>?>(null);
        }

        public Task SetAsync<T>(
            string cacheType,
            string cacheKey,
            T value,
            DateTimeOffset expiresAtUtc,
            CancellationToken cancellationToken = default)
        {
            WriteCount++;
            return Task.CompletedTask;
        }
    }
}
