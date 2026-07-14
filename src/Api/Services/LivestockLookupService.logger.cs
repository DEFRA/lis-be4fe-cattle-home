namespace Defra.Lis.Be4Fe.Api.Services;

using Defra.Lis.Be4Fe.Api.Foundation.Caching;
using Defra.Lis.Be4Fe.Api.Lookups.Providers;
using Defra.Lis.CattleHome.Integrations.CattleApi;
using Defra.Lis.CattleHome.Lookups.Models;
using Microsoft.Extensions.Options;

public sealed class LivestockLookupService(
    ICachedDataService cachedDataService,
    IUserCphProvider userCphProvider,
    ICattleApiClient cattleApiClient,
    ILogger<LivestockLookupService> logger)
    : ILivestockLookupService
{
    [LoggerMessage(LogLevel.Debug, "Cache hit for user CPHs. UserId: {UserId}")]
    private partial void LogCphsForUserCacheHit(string userId);

    [LoggerMessage(LogLevel.Debug, "Cache miss for user CPHs. Retrieving from provider. UserId: {UserId}")]
    private partial void LogCphsForUserCacheMiss(string userId);

    [LoggerMessage(LogLevel.Information, "Retrieved {Count} CPHs for user. UserId: {UserId}")]
    private partial void LogCphsForUserRetrieved(int count, string userId);

    [LoggerMessage(LogLevel.Debug, "Cache hit for CPH cattle. Cph: {Cph}")]
    private partial void LogCattleForCphCacheHit(string cph);

    [LoggerMessage(LogLevel.Debug, "Cache miss for CPH cattle. Retrieving from API. Cph: {Cph}")]
    private partial void LogCattleForCphCacheMiss(string cph);

    [LoggerMessage(LogLevel.Information, "Retrieved {Count} cattle for CPH. Cph: {Cph}")]
    private partial void LogCattleForCphRetrieved(int count, string cph);

    [LoggerMessage(LogLevel.Debug, "Cache hit for cattle details. CattleId: {CattleId}")]
    private partial void LogCattleDetailsCacheHit(string cattleId);

    [LoggerMessage(LogLevel.Debug, "Cache miss for cattle details. Retrieving from API. CattleId: {CattleId}")]
    private partial void LogCattleDetailsCacheMiss(string cattleId);

    [LoggerMessage(LogLevel.Information, "Retrieved cattle details. CattleId: {CattleId}")]
    private partial void LogCattleDetailsRetrieved(string cattleId);
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
            LogCphsForUserCacheHit(normalisedUserId);
            return cached;
        }

        LogCphsForUserCacheMiss(normalisedUserId);
        var cphs = (await userCphProvider.GetCphsForUserAsync(normalisedUserId, cancellationToken)).ToList();
        LogCphsForUserRetrieved(cphs.Count, normalisedUserId);

        return await cachedDataService
            .StoreAndWrapAsync(
                "user-cphs",
                normalisedUserId,
                cphs,
                "cph-provider",
                cancellationToken);
    }

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
            LogCattleForCphCacheHit(normalisedCph);
            return cached;
        }

        LogCattleForCphCacheMiss(normalisedCph);
        var cattle = (await cattleApiClient.GetCattleForCphAsync(normalisedCph, cancellationToken)).ToList();
        LogCattleForCphRetrieved(cattle.Count, normalisedCph);

        return await cachedDataService
            .StoreAndWrapAsync(
                "cph-cattle",
                normalisedCph,
                cattle,
                "cattle",
                cancellationToken);
    }

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
            LogCattleDetailsCacheHit(normalisedCattleId);
            return cached;
        }

        LogCattleDetailsCacheMiss(normalisedCattleId);
        var details = await cattleApiClient
            .GetCattleDetailsAsync(normalisedCattleId, cancellationToken);
        LogCattleDetailsRetrieved(normalisedCattleId);

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
