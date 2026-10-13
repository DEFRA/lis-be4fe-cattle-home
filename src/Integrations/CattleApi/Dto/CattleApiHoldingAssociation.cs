// <copyright file="CattleApiHoldingAssociation.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi.Dto;

public sealed record CattleApiHoldingAssociation(
    string? CustomerNumber,
    string? Title,
    string? FirstName,
    string? LastName,
    string? Name,
    string? PartyType,
    string? Email,
    string? Mobile,
    string? Telephone,
    IReadOnlyList<string>? Address,
    IReadOnlyList<CattleApiHoldingRole>? Roles);
