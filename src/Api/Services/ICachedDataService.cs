namespace Defra.Lis.Be4Fe.Api.Services;

using Defra.Lis.CattleHome.Lookups.Models;

public interface ICachedDataService
{
    Task<CachedLookupResponse<T>?> TryGetCachedAsync<T>(
        string cacheType,
        string cacheKey,
        CancellationToken cancellationToken);

    Task<CachedLookupResponse<T>> StoreAndWrapAsync<T>(
        string cacheType,
        string cacheKey,
        T value,
        string source,
        CancellationToken cancellationToken);
}
