// <copyright file="MongoExternalDataCacheRepository.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Foundation.Caching;

using System.Text.Json;
using Defra.Lis.Be4Fe.Api.Utils.Mongo;
using MongoDB.Driver;

public sealed class MongoExternalDataCacheRepository : IExternalDataCacheRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IMongoCollection<ExternalDataCacheDocument> collection;
    private readonly ILogger<MongoExternalDataCacheRepository> logger;
    private int indexesInitialised;

    public MongoExternalDataCacheRepository(
        IMongoDbClientFactory mongoDbClientFactory,
        ILogger<MongoExternalDataCacheRepository> logger)
    {
        collection = mongoDbClientFactory.GetCollection<ExternalDataCacheDocument>("externalDataCache");
        this.logger = logger;
    }

    public async Task<CacheLookupResult<T>?> GetAsync<T>(
        string cacheType,
        string cacheKey,
        CancellationToken cancellationToken = default)
    {
        await EnsureIndexesAsync(cancellationToken);

        var normalisedKey = CreateId(cacheType, cacheKey);
        var now = DateTime.UtcNow;
        var filter = Builders<ExternalDataCacheDocument>.Filter.Where(entry =>
            entry.Id == normalisedKey && entry.ExpiresAtUtc > now);

        var entry = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        if (entry is null)
        {
            return null;
        }

        return new CacheLookupResult<T>
        {
            Value = JsonSerializer.Deserialize<T>(entry.PayloadJson, SerializerOptions)
                ?? throw new InvalidOperationException($"Could not deserialize cached payload for {normalisedKey}."),
            ExpiresAtUtc = new DateTimeOffset(DateTime.SpecifyKind(entry.ExpiresAtUtc, DateTimeKind.Utc)),
        };
    }

    public async Task SetAsync<T>(
        string cacheType,
        string cacheKey,
        T value,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        await EnsureIndexesAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var normalisedKey = CreateId(cacheType, cacheKey);
        var document = new ExternalDataCacheDocument
        {
            Id = normalisedKey,
            CacheType = cacheType,
            CacheKey = cacheKey,
            PayloadJson = JsonSerializer.Serialize(value, SerializerOptions),
            CreatedAtUtc = now,
            ExpiresAtUtc = expiresAtUtc.UtcDateTime,
        };

        await collection.ReplaceOneAsync(
            entry => entry.Id == normalisedKey,
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    private static string CreateId(string cacheType, string cacheKey)
    {
        return $"{cacheType}:{cacheKey}";
    }

    private async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref indexesInitialised, 1) == 1)
        {
            return;
        }

        var expiryIndex = new CreateIndexModel<ExternalDataCacheDocument>(
            Builders<ExternalDataCacheDocument>.IndexKeys.Ascending(entry => entry.ExpiresAtUtc),
            new CreateIndexOptions { ExpireAfter = TimeSpan.Zero, Name = "ttl_expiresAtUtc" });

        try
        {
            await collection.Indexes.CreateOneAsync(expiryIndex, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to create Mongo cache indexes. Continuing without ensuring indexes.");
        }
    }
}
