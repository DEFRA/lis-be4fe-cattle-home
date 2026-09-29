// <copyright file="CattleApiUserDetails.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi.Dto;

public sealed record CattleApiUserDetails(
    string? Subject,
    string? Email,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    IReadOnlyList<CattleApiUserCph>? Cphs);
