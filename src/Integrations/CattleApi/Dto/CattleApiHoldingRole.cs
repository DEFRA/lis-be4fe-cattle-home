// <copyright file="CattleApiHoldingRole.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi.Dto;

public sealed record CattleApiHoldingRole(
    string? Code,
    IReadOnlyList<string>? Species);
