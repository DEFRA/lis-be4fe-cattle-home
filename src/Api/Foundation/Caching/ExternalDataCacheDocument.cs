// <copyright file="ExternalDataCacheDocument.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Foundation.Caching;

using MongoDB.Bson.Serialization.Attributes;

public sealed class ExternalDataCacheDocument
{
    [BsonId]
    public required string Id { get; init; }

    public required string CacheType { get; init; }

    public required string CacheKey { get; init; }

    public required string PayloadJson { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public required DateTime ExpiresAtUtc { get; init; }
}
