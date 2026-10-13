// <copyright file="CattleLocation.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi;

public sealed class CattleLocation
{
    public string? Identifier { get; init; }

    public string? SubIdentifier { get; init; }

    public string? FullIdentifier { get; init; }

    public string? Name { get; init; }

    public string? PremisesType { get; init; }
}
