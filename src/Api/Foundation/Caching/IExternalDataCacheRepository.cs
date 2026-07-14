// <copyright file="IExternalDataCacheRepository.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Foundation.Caching;

public interface IExternalDataCacheRepository
{
    Task<CacheLookupResult<T>?> GetAsync<T>(string cacheType, string cacheKey, CancellationToken cancellationToken = default);

    Task SetAsync<T>(
        string cacheType,
        string cacheKey,
        T value,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default);
}
