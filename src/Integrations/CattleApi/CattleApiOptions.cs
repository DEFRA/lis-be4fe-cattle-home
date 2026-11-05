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
    /// Gets the API key sent to the cattle API as <c>x-api-key</c>. Required: the cattle API rejects calls without it.
    /// </summary>
    public string? ApiKey { get; init; }

    /// <summary>
    /// Gets the timeout in milliseconds for getting holding details (min 1 ms, max 5 minutes / 300000 ms).
    /// </summary>
    public int GetHoldingDetailsTimeout { get; init; } = 60_000;

    /// <summary>
    /// Gets the timeout in milliseconds for searching holdings (min 1 ms, max 5 minutes / 300000 ms).
    /// </summary>
    public int SearchHoldingsTimeout { get; init; } = 60_000;

    /// <summary>
    /// Gets the timeout in milliseconds for searching cattle on a holding (min 1 ms, max 5 minutes / 300000 ms).
    /// </summary>
    public int SearchCattleTimeout { get; init; } = 60_000;

    /// <summary>
    /// Gets the timeout in milliseconds for getting cattle details (min 1 ms, max 5 minutes / 300000 ms).
    /// </summary>
    public int GetCattleDetailsTimeout { get; init; } = 60_000;

    /// <summary>
    /// Gets the timeout in milliseconds for getting user details (min 1 ms, max 5 minutes / 300000 ms).
    /// </summary>
    public int GetUserDetailsTimeout { get; init; } = 60_000;

    /// <summary>
    /// Gets the timeout in milliseconds for creating a user (min 1 ms, max 5 minutes / 300000 ms).
    /// </summary>
    public int CreateUserTimeout { get; init; } = 60_000;
}
