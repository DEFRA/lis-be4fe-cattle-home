// <copyright file="UserLookupService.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Services;

using Defra.Lis.Be4Fe.CattleApi;
using Defra.Lis.Be4Fe.Models.Lookups.Models;

public sealed partial class UserLookupService(
    ICachedDataService cachedDataService,
    ICattleApiClient cattleApiClient,
    ILogger<UserLookupService> logger)
    : IUserLookupService
{
    public async Task<CachedLookupResponse<List<UserCph>>> GetCphsForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var normalisedUserId = Normalise(userId);
        var cached = await cachedDataService
            .TryGetCachedAsync<List<UserCph>>(
                "user-cphs",
                normalisedUserId,
                cancellationToken);

        if (cached is not null)
        {
            LogCacheHitForUserCphsUseridUserid(normalisedUserId);
            return cached;
        }

        LogCacheMissForUserCphsRetrievingFromProviderUseridUserid(normalisedUserId);
        var cphs = (await cattleApiClient.GetCphsForUserAsync(normalisedUserId, cancellationToken)).ToList();
        LogRetrievedCountCphsForUserUseridUserid(cphs.Count, normalisedUserId);

        return await cachedDataService
            .StoreAndWrapAsync(
                "user-cphs",
                normalisedUserId,
                cphs,
                "cph-provider",
                cancellationToken);
    }

    private static string Normalise(string value)
    {
        return value.Trim().ToUpperInvariant();
    }
}
