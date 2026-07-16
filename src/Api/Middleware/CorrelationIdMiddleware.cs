// <copyright file="CorrelationIdMiddleware.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Middleware;

using Defra.Lis.Be4Fe.Api.MetaData;
using Defra.Lis.Be4Fe.Api.Middleware.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public partial class CorrelationIdMiddleware(ILogger<CorrelationIdMiddleware> logger) : JsonErrorMiddleware
{
    public override async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // check if this maps to an endpoint. If not, just call the next middleware.
        var endpoint = context.GetEndpoint();
        if (endpoint == null)
        {
            await next(context);
            return;
        }

        // check if the endpoint has the IgnoreCorrelationIdCheck metadata. If so, skip the API key check.
        var ignoreCorrelationIdCheck = endpoint.Metadata.GetMetadata<IgnoreCorrelationIdCheck>() is not null;
        if (ignoreCorrelationIdCheck)
        {
            await next(context);
            return;
        }

        try
        {
            var headers = context.Request.Headers;
            var correlationId = headers.TryGetValue(RequestHeaderNames.CorrelationId, out var tid) ? tid.ToString() : null;
            correlationId = NormalizeHeaderValue(correlationId);
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                await WriteJsonErrorAsync(
                    context,
                    statusCode: StatusCodes.Status400BadRequest,
                    code: "missing_header",
                    message: $"Header {RequestHeaderNames.CorrelationId} is required.",
                    details: new { header = $"{RequestHeaderNames.CorrelationId}" });
                return;
            }

            await next(context);
        }
        catch (Exception ex)
        {
            LogErrorInMiddleware(logger, nameof(CorrelationIdMiddleware), ex);
            throw;
        }
    }

    private static string? NormalizeHeaderValue(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();

        // Treat empty quotes as "missing": "", '' (and also values with spaces like "  ").
        trimmed = trimmed.Trim('\"', '\'');

        trimmed = trimmed.Trim();

        return trimmed.Length == 0 ? null : trimmed;
    }
}
