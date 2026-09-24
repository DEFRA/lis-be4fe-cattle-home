// <copyright file="CattleApiCattleDetails.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi.Dto;

/// <summary>
/// One animal's details from the cattle API's cattle-details response (camelCase JSON). The
/// parentage CADS holds as a list arrives here already flattened into the dam and sire fields.
/// </summary>
public sealed record CattleApiCattleDetails(
    string? EarTag,
    string? Species,
    string? Sex,
    DateOnly? DateBirth,
    DateOnly? DateRegistered,
    DateOnly? DateOnCph,
    string? Breed,
    string? BreedCode,
    string? BreedName,
    string? State,
    string? RestrictionStatus,
    string? DamType,
    string? GeneticDamEarTag,
    string? SurrogateDamEarTag,
    string? SireEarTag,
    string? SireName);
