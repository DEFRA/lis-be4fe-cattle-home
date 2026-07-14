// <copyright file="CattleLookupService.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Services;

using Defra.Lis.Be4Fe.CattleApi;
using Defra.Lis.Be4Fe.Models.Lookups.Models;

public sealed partial class CattleLookupService(
    ICachedDataService cachedDataService,
    ICattleApiClient cattleApiClient,
    ILogger<CattleLookupService> logger)
    : ICattleLookupService
{
    public async Task<CachedLookupResponse<CattleDetails>> GetCattleDetailsAsync(string cattleId, CancellationToken cancellationToken = default)
    {
        var normalisedCattleId = Normalise(cattleId);
        var cached = await cachedDataService
            .TryGetCachedAsync<CattleDetails>(
                "cattle-details",
                normalisedCattleId,
                cancellationToken);

        if (cached is not null)
        {
            LogCacheHitForCattleDetailsCattleidCattleid(normalisedCattleId);
            return cached;
        }

        LogCacheMissForCattleDetailsRetrievingFromApiCattleidCattleid(normalisedCattleId);
        var details = await cattleApiClient.GetCattleDetailsAsync(normalisedCattleId, cancellationToken);
        LogRetrievedCattleDetailsCattleidCattleid(normalisedCattleId);

        return await cachedDataService
            .StoreAndWrapAsync(
                "cattle-details",
                normalisedCattleId,
                details,
                "cattle",
                cancellationToken);
    }

    private static string Normalise(string value)
    {
        return value.Trim().ToUpperInvariant();
    }
}
