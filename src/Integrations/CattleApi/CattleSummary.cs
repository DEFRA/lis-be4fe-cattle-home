// <copyright file="CattleSummary.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi;

public sealed class CattleSummary
{
    public required string CattleId { get; init; }

    public required string Eartag { get; init; }

    public required string Breed { get; init; }

    public required string Sex { get; init; }
}
