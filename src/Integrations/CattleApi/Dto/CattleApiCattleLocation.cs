// <copyright file="CattleApiCattleLocation.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi.Dto;

public sealed record CattleApiCattleLocation(
    string? Identifier,
    string? SubIdentifier,
    string? FullIdentifier,
    string? Name,
    string? PremisesType);
