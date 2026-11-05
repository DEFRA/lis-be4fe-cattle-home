// <copyright file="CattleApiOptions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi;

using System.ComponentModel.DataAnnotations;

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
    /// Gets how long, in seconds, to wait for the cattle API's response headers before giving up. Shorter than the
    /// cattle-home app's 5 s budget for the BE4FE, and longer than the cattle API's own budget for CADS and KRDS, so the
    /// cattle API can answer 503 first. A stalled response body is cut off one second later by the HttpClient timeout.
    /// </summary>
    [Range(1, 300)]
    public int TimeoutSeconds { get; init; } = 4;

    /// <summary>
    /// Gets how many requests must fail without a response (transport failure or timeout), with none answered in
    /// between, within <see cref="CircuitBreakerSamplingSeconds"/> before the circuit opens and calls fail fast.
    /// </summary>
    [Range(2, 1000)]
    public int CircuitBreakerFailureThreshold { get; init; } = 5;

    /// <summary>
    /// Gets the window, in seconds, over which <see cref="CircuitBreakerFailureThreshold"/> failures are counted.
    /// </summary>
    [Range(1, 3600)]
    public int CircuitBreakerSamplingSeconds { get; init; } = 30;

    /// <summary>
    /// Gets how long, in seconds, the circuit stays open (failing fast) before one probe request is let through.
    /// </summary>
    [Range(1, 3600)]
    public int CircuitBreakerBreakSeconds { get; init; } = 30;
}
