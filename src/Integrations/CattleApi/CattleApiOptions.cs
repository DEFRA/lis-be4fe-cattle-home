// <copyright file="CattleApiOptions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi;

public sealed class CattleApiOptions
{
    public const string SectionName = "CattleApi";

    /// <summary>
    /// Gets the base URL of the cattle API (lis-api-cattle). Required for the holding and cattle lookups.
    /// </summary>
    public string? BaseUrl { get; init; }

    /// <summary>
    /// Gets the API key sent to the cattle API as <c>x-api-key</c>; omitted from requests when not configured.
    /// </summary>
    public string? ApiKey { get; init; }

    /// <summary>
    /// Gets the fixture file backing the user CPH and cattle detail lookups until the cattle API provides them.
    /// </summary>
    public string FixturePath { get; init; } = "Fixtures/CattleApi/cattle.json";
}
