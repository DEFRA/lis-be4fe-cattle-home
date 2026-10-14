// <copyright file="CattleApiHolding.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi.Dto;

/// <summary>
/// One holding from the cattle API's paginated holdings search response (camelCase JSON).
/// </summary>
public record CattleApiHolding(
    string? Cph,
    string? Name,
    string? HoldingType,
    IReadOnlyList<string>? Address,
    string? KeeperName,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    bool? Active);
