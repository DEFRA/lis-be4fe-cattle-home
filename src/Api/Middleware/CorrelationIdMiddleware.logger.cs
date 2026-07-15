// <copyright file="CorrelationIdMiddleware.logger.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Middleware;

public partial class CorrelationIdMiddleware
{
    [LoggerMessage(LogLevel.Error, "Error in {MiddlewareName}")]
    static partial void LogErrorInMiddleware(ILogger logger, string middlewareName, Exception exception);
}
