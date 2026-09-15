// <copyright file="CphLookupService.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Services;

using Defra.Lis.Be4Fe.CattleApi;
using Defra.Lis.Be4Fe.Models.Lookups.Models;

/// <summary>
/// Cattle on a holding, passed straight through to the cattle API (no caching in this phase).
/// </summary>
public sealed partial class CphLookupService(
    ICattleHoldingClient cattleHoldingClient,
    ILogger<CphLookupService> logger)
    : ICphLookupService
{
    public async Task<CachedLookupResponse<List<CattleSummary>>> GetCattleForCphAsync(string cph, CattleSearchQuery? query = null, CancellationToken cancellationToken = default)
    {
        var normalisedCph = Normalise(cph);
        var cattle = (await cattleHoldingClient.SearchCattleAsync(normalisedCph, query ?? new CattleSearchQuery(), cancellationToken)).ToList();
        LogRetrievedCountCattleForCphCphCph(cattle.Count, normalisedCph);

        return new CachedLookupResponse<List<CattleSummary>>
        {
            Source = LookupSources.CattleApi,
            CachedUntilUtc = null,
            Data = cattle,
        };
    }

    private static string Normalise(string value)
    {
        return value.Trim().ToUpperInvariant();
    }
}
