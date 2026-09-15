// <copyright file="HoldingDetails.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Lookups.Models;

/// <summary>
/// Holding details for a CPH, shaped like <see cref="UserCph"/> so the cattle-home UI can read
/// either with the same field names.
/// </summary>
public sealed class HoldingDetails
{
    public required string Cph { get; init; }

    public string? Name { get; init; }

    public string? BusinessName { get; init; }

    public IReadOnlyCollection<string> Address { get; init; } = [];

    public string? HoldingType { get; init; }

    public string? RegisteredKeeper { get; init; }

    public IReadOnlyCollection<string> HerdMarks { get; init; } = [];

    public IReadOnlyCollection<string> AllowedSpecies { get; init; } = [];
}
