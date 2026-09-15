// <copyright file="HoldingLookupService.logger.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Services;

public partial class HoldingLookupService
{
    [LoggerMessage(LogLevel.Information, "Retrieved holding details for CPH. Cph: {Cph}")]
    partial void LogRetrievedHoldingDetailsCphCph(string cph);
}
