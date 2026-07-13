// <copyright file="CattleDetails.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.CattleHome.Integrations.CattleApi;

public sealed class CattleDetails
{
    public required string CattleId { get; init; }

    public required string Eartag { get; init; }

    public required string Cph { get; init; }

    public required string Breed { get; init; }

    public required string Sex { get; init; }

    public required DateOnly DateOfBirth { get; init; }

    public required string Status { get; init; }
}
