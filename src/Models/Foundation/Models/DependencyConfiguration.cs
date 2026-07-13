// <copyright file="DependencyConfiguration.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.CattleHome.Foundation.Models;

public sealed class DependencyConfiguration
{
    public required DependencyStatus Mongo { get; init; }

    public required DependencyStatus CattleApi { get; init; }
}
