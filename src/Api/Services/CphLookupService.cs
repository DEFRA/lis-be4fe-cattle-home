// <copyright file="CphLookupService.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Services;

using Defra.Lis.Be4Fe.CattleApi;
using Defra.Lis.Be4Fe.Models.Lookups.Models;

public sealed partial class CphLookupService(
    ICachedDataService cachedDataService,
    ICattleApiClient cattleApiClient,
    ILogger<CphLookupService> logger)
    : ICphLookupService
{
    public async Task<CachedLookupResponse<List<CattleSummary>>> GetCattleForCphAsync(string cph, CancellationToken cancellationToken = default)
    {
        var normalisedCph = Normalise(cph);
        var cached = await cachedDataService
            .TryGetCachedAsync<List<CattleSummary>>(
                "cph-cattle",
                normalisedCph,
                cancellationToken);

        if (cached is not null)
        {
            LogCacheHitForCphCattleCphCph(normalisedCph);
            return cached;
        }

        LogCacheMissForCphCattleRetrievingFromApiCphCph(normalisedCph);
        var cattle = (await cattleApiClient.GetCattleForCphAsync(normalisedCph, cancellationToken)).ToList();
        LogRetrievedCountCattleForCphCphCph(cattle.Count, normalisedCph);

        return await cachedDataService
            .StoreAndWrapAsync(
                "cph-cattle",
                normalisedCph,
                cattle,
                "cattle",
                cancellationToken);
    }

    private static string Normalise(string value)
    {
        return value.Trim().ToUpperInvariant();
    }
}
