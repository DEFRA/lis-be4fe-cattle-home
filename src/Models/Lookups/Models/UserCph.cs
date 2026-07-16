// <copyright file="UserCph.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Lookups.Models;

public sealed class UserCph
{
    public required string Cph { get; init; }

    public required string Name { get; init; }

    public IReadOnlyCollection<string> AllowedSpecies { get; init; } = [];

    public string? Postcode { get; init; }

    public decimal? Latitude { get; init; }

    public decimal? Longitude { get; init; }
}
