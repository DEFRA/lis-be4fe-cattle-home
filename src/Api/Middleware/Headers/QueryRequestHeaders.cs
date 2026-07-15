// <copyright file="QueryRequestHeaders.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Middleware.Headers;

using System.Reflection;
using Microsoft.AspNetCore.Http;

public sealed record QueryRequestHeaders(Guid CorrelationId, string ApiKey)
{
    public static readonly object ItemKey = new();

    public static bool TryGet(HttpContext context, out QueryRequestHeaders headers)
    {
        if (context.Items.TryGetValue(ItemKey, out var value) && value is QueryRequestHeaders h)
        {
            headers = h;
            return true;
        }

        headers = null!;
        return false;
    }

    public static ValueTask<QueryRequestHeaders> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        // Prefer whatever the middleware already validated/created.
        if (TryGet(context, out var existing))
        {
            return ValueTask.FromResult(existing);
        }

        var headers = context.Request.Headers;

        var correlationId =
            headers.TryGetValue(RequestHeaderNames.CorrelationId, out var tid) ? tid.ToString() : null;

        if (correlationId == null || !Guid.TryParse(correlationId, out _))
        {
            throw new BadHttpRequestException(
                $"Header {RequestHeaderNames.CorrelationId} is required.",
                StatusCodes.Status400BadRequest);
        }

        var apiKey =
            headers.TryGetValue(RequestHeaderNames.ApiKey, out var key) ? key.ToString() : null;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new BadHttpRequestException(
                $"Header {RequestHeaderNames.ApiKey} is required.",
                StatusCodes.Status400BadRequest);
        }

        return ValueTask.FromResult(new QueryRequestHeaders(
            CorrelationId: Guid.Parse(correlationId),
            ApiKey: apiKey));
    }
}
