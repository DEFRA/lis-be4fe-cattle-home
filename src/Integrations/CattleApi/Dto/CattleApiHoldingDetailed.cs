// <copyright file="CattleApiHoldingDetailed.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi.Dto;

/// <summary>
/// The cattle API's holding detailed response (camelCase JSON).
/// </summary>
public sealed record CattleApiHoldingDetailed(
    string? Cph,
    string? Name,
    string? HoldingType,
    IReadOnlyList<string>? Address,
    string? KeeperName,
    IReadOnlyList<CattleApiHoldingMark>? HerdMarks,
    IReadOnlyList<string>? AllowedSpecies,
    IReadOnlyList<CattleApiHoldingAssociation>? Associations);
