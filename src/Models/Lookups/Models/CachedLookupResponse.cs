// <copyright file="CachedLookupResponse.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.CattleHome.Lookups.Models;

public sealed class CachedLookupResponse<T>
{
    public required string Source { get; init; }

    public required DateTimeOffset CachedUntilUtc { get; init; }

    public required T Data { get; init; }
}
