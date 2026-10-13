// <copyright file="HoldingRoleResponse.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Lookups.Models;

public sealed class HoldingRoleResponse
{
    public string? Code { get; init; }

    public IReadOnlyCollection<string> Species { get; init; } = [];
}
