// <copyright file="HoldingMarkResponse.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Lookups.Models;

public sealed class HoldingMarkResponse
{
    public string? Mark { get; init; }

    public DateTimeOffset? StartDate { get; init; }

    public DateTimeOffset? EndDate { get; init; }

    public IReadOnlyCollection<string> Species { get; init; } = [];
}
