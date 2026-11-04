// <copyright file="CattleApiHttpClient.logger.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Utils.Http;

using Microsoft.Extensions.Logging;

/// <summary>
/// The circuit transition log messages for <see cref="CattleApiHttpClient"/>.
/// </summary>
public static partial class CattleApiHttpClient
{
    [LoggerMessage(LogLevel.Warning, "The cattle API circuit opened: at least {FailureCount} calls failed without a response and none succeeded. Calls fail fast for {BreakSeconds}s.")]
    private static partial void LogCircuitOpened(ILogger logger, int failureCount, double breakSeconds);

    [LoggerMessage(LogLevel.Warning, "The cattle API circuit reopened: the probe call failed without a response. Calls fail fast for {BreakSeconds}s.")]
    private static partial void LogCircuitReopened(ILogger logger, double breakSeconds);

    [LoggerMessage(LogLevel.Information, "The cattle API circuit is half-open: letting one probe call through.")]
    private static partial void LogCircuitHalfOpened(ILogger logger);

    [LoggerMessage(LogLevel.Information, "The cattle API circuit closed: the cattle API is answering again.")]
    private static partial void LogCircuitClosed(ILogger logger);
}
