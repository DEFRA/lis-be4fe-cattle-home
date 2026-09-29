// <copyright file="CattleHoldingRestClient.logger.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi;

using Microsoft.Extensions.Logging;

public partial class CattleHoldingRestClient
{
    [LoggerMessage(LogLevel.Information, "Retrieved holding details from the cattle API. Cph: {Cph}")]
    partial void LogRetrievedHoldingDetails(string cph);

    [LoggerMessage(LogLevel.Information, "Retrieved {Count} cattle from the cattle API. Cph: {Cph}, Filtered: {Filtered}")]
    partial void LogRetrievedCattleForHolding(int count, string cph, bool filtered);

    [LoggerMessage(LogLevel.Warning, "The cattle API does not know the holding. Cph: {Cph}")]
    partial void LogHoldingNotFound(string cph);

    [LoggerMessage(LogLevel.Information, "Retrieved cattle details from the cattle API. EarTag: {EarTag}")]
    partial void LogRetrievedCattleDetails(string earTag);

    [LoggerMessage(LogLevel.Warning, "The cattle API does not know the animal. EarTag: {EarTag}")]
    partial void LogCattleNotFound(string earTag);

    [LoggerMessage(LogLevel.Information, "Retrieved user details with {Count} CPHs from the cattle API. UserId: {UserId}")]
    partial void LogRetrievedUserDetails(int count, string userId);

    [LoggerMessage(LogLevel.Warning, "The cattle API does not know the user. UserId: {UserId}")]
    partial void LogUserNotFound(string userId);
}
