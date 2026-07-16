// <copyright file="CachedLookupResponse.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Lookups.Models;

public sealed class CachedLookupResponse<T>
{
    public required string Source { get; init; }

    public DateTimeOffset? CachedUntilUtc { get; init; }

    public required T Data { get; init; }
}
