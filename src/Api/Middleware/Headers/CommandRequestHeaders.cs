// <copyright file="CommandRequestHeaders.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Middleware.Headers;

using System.Reflection;
using Microsoft.AspNetCore.Http;

public sealed record CommandRequestHeaders(Guid CorrelationId, Guid OperatorId, string ApiKey)
{
    public static readonly object ItemKey = new();

    public static bool TryGet(HttpContext context, out CommandRequestHeaders headers)
    {
        if (context.Items.TryGetValue(ItemKey, out var value) && value is CommandRequestHeaders h)
        {
            headers = h;
            return true;
        }

        headers = default!;
        return false;
    }

    public static ValueTask<CommandRequestHeaders> BindAsync(HttpContext context, ParameterInfo parameter)
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

        var operatorId =
            headers.TryGetValue(RequestHeaderNames.OperatorId, out var oid) ? oid.ToString() : null;

        if (operatorId == null || !Guid.TryParse(operatorId, out _))
        {
            throw new BadHttpRequestException(
                $"Header {RequestHeaderNames.OperatorId} is required.",
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

        return ValueTask.FromResult(new CommandRequestHeaders(
            CorrelationId: Guid.Parse(correlationId),
            OperatorId: Guid.Parse(operatorId),
            ApiKey: apiKey));
    }
}
