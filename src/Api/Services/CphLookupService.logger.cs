// <copyright file="CphLookupService.logger.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Services;

public partial class CphLookupService
{
    [LoggerMessage(LogLevel.Information, "Retrieved {Count} cattle for CPH. Cph: {Cph}")]
    partial void LogRetrievedCountCattleForCphCphCph(int count, string cph);
}
