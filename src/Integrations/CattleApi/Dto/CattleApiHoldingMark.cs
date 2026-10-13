// <copyright file="CattleApiHoldingMark.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi.Dto;

public sealed record CattleApiHoldingMark(
    string? Mark,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    IReadOnlyList<string>? Species);
