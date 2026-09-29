// <copyright file="CattleApiUserCph.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi.Dto;

public sealed record CattleApiUserCph(
    string? Cph,
    string? HoldingId,
    string? HoldingName,
    string? Role);
