// <copyright file="CachedDataService.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Services;

using Defra.Lis.Be4Fe.Api.Foundation.Caching;
using Defra.Lis.CattleHome.Lookups.Models;
using Microsoft.Extensions.Options;

public class CachedDataService(
    IExternalDataCacheRepository cacheRepository,
    IOptions<CacheOptions> cacheOptions,
    TimeProvider timeProvider,
    ILogger<CachedDataService> logger) :
    ICachedDataService
{
    public async Task<CachedLookupResponse<T>?> TryGetCachedAsync<T>(
        string cacheType,
        string cacheKey,
        CancellationToken cancellationToken)
    {
        try
        {
            var cached = await cacheRepository.GetAsync<T>(cacheType, cacheKey, cancellationToken);

            if (cached is null)
            {
                return null;
            }

            return new CachedLookupResponse<T>
            {
                Source = "cache",
                CachedUntilUtc = cached.ExpiresAtUtc,
                Data = cached.Value,
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache read failed for {CacheType} {CacheKey}. Falling back to provider.", cacheType, cacheKey);
            return null;
        }
    }

    public async Task<CachedLookupResponse<T>> StoreAndWrapAsync<T>(
        string cacheType,
        string cacheKey,
        T value,
        string source,
        CancellationToken cancellationToken)
    {
        var cachedUntilUtc = timeProvider.GetUtcNow().AddMinutes(cacheOptions.Value.DurationMinutes);

        try
        {
            await cacheRepository.SetAsync(cacheType, cacheKey, value, cachedUntilUtc, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache write failed for {CacheType} {CacheKey}. Returning provider data without cache.", cacheType, cacheKey);
        }

        return new CachedLookupResponse<T>
        {
            Source = source,
            CachedUntilUtc = cachedUntilUtc,
            Data = value,
        };
    }
}
