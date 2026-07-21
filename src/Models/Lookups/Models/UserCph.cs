// <copyright file="UserCph.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Lookups.Models;

public sealed class UserCph
{
    public required string Cph { get; init; }

    public required string Name { get; init; }

    public required string BusinessName { get; init; }

    public IReadOnlyCollection<string> Address { get; init; } = [];

    public IReadOnlyCollection<string> AllowedSpecies { get; init; } = [];

    public string? HoldingType { get; init; }

    public string? RegisteredKeeper { get; init; }

    public IReadOnlyCollection<string> HerdMarks { get; init; } = [];

    public string? Postcode { get; init; }

    public decimal? Latitude { get; init; }

    public decimal? Longitude { get; init; }
}
