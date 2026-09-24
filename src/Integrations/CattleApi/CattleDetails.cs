// <copyright file="CattleDetails.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi;

public sealed class CattleDetails
{
    public required string CattleId { get; init; }

    public required string Eartag { get; init; }

    /// <summary>
    /// Gets the holding the animal is on. CADS does not return it with an animal's details, so it
    /// is null until the upstream contract carries it.
    /// </summary>
    public string? Cph { get; init; }

    /// <summary>
    /// Gets the breed code, for example "AA". The UI resolves it to a name for display.
    /// </summary>
    public string? Breed { get; init; }

    public string? BreedName { get; init; }

    public string? Species { get; init; }

    public string? Sex { get; init; }

    public DateOnly? DateOfBirth { get; init; }

    public DateOnly? DateRegistered { get; init; }

    public DateOnly? DateOnCph { get; init; }

    /// <summary>
    /// Gets the animal's state, "Alive" or "Dead".
    /// </summary>
    public string? State { get; init; }

    /// <summary>
    /// Gets the animal's restriction status, for example "None" or "Restricted".
    /// </summary>
    public string? RestrictionStatus { get; init; }

    /// <summary>
    /// Gets the kind of dam recorded, "surrogate" or "genetic", or null when none is.
    /// </summary>
    public string? DamType { get; init; }

    public string? GeneticDamTag { get; init; }

    public string? SurrogateTag { get; init; }

    public string? SireTag { get; init; }

    public string? SireName { get; init; }
}
