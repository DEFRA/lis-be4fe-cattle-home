// <copyright file="CattleApiHolding.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi.Dto;

/// <summary>
/// The cattle API's holding response (camelCase JSON).
/// </summary>
public sealed record CattleApiHolding(
    string? Cph,
    string? Name,
    string? HoldingType,
    IReadOnlyList<string>? Address,
    string? KeeperName,
    IReadOnlyList<string>? HerdMarks,
    IReadOnlyList<string>? AllowedSpecies);
