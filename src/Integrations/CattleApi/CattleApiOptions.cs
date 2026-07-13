// <copyright file="CattleApiOptions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.CattleHome.Integrations.CattleApi;

public sealed class CattleApiOptions
{
    public const string SectionName = "CattleApi";

    public string? BaseUrl { get; init; }
}
