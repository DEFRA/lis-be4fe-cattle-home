// <copyright file="CphLookupService.logger.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Services;

public partial class CphLookupService
{
    [LoggerMessage(LogLevel.Debug, "Cache hit for CPH cattle. Cph: {Cph}")]
    partial void LogCacheHitForCphCattleCphCph(string cph);

    [LoggerMessage(LogLevel.Debug, "Cache miss for CPH cattle. Retrieving from API. Cph: {Cph}")]
    partial void LogCacheMissForCphCattleRetrievingFromApiCphCph(string cph);

    [LoggerMessage(LogLevel.Information, "Retrieved {Count} cattle for CPH. Cph: {Cph}")]
    partial void LogRetrievedCountCattleForCphCphCph(int count, string cph);
}
