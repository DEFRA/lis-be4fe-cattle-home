// <copyright file="ApiKeyValidationMiddleware.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Middleware;

using Defra.Lis.Be4Fe.Api.MetaData;
using Defra.Lis.Be4Fe.Api.Middleware.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public partial class ApiKeyValidationMiddleware(string apiKey, ILogger<ApiKeyValidationMiddleware> logger)
    : JsonErrorMiddleware
{
    public override async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ArgumentException.ThrowIfNullOrEmpty(apiKey);

        // check if this maps to an endpoint. If not, just call the next middleware.
        var endpoint = context.GetEndpoint();
        if (endpoint == null)
        {
            await next(context);
            return;
        }

        // check if the endpoint has the IgnoreApiKeyCheck metadata. If so, skip the API key check.
        var ignoreApiKeyCheck = endpoint.Metadata.GetMetadata<IgnoreApiKeyCheck>() is not null;
        if (ignoreApiKeyCheck)
        {
            await next(context);
            return;
        }

        try
        {
            var headers = context.Request.Headers;
            headers.TryGetValue(RequestHeaderNames.ApiKey, out var key);
            if (string.IsNullOrWhiteSpace(key))
            {
                await WriteJsonErrorAsync(
                    context,
                    statusCode: StatusCodes.Status400BadRequest,
                    code: "missing_header",
                    message: $"Header {RequestHeaderNames.ApiKey} is required.",
                    details: new { header = $"{RequestHeaderNames.ApiKey}" });
                return;
            }

            if (!string.Equals(key, apiKey, StringComparison.Ordinal))
            {
                await WriteJsonErrorAsync(
                    context,
                    statusCode: StatusCodes.Status400BadRequest,
                    code: "invalid_api_key",
                    message: $"Header {RequestHeaderNames.ApiKey} is not valid.",
                    details: new { header = $"{RequestHeaderNames.ApiKey}" });
                return;
            }

            await next(context);
        }
        catch (Exception ex)
        {
            LogErrorInMiddleware(logger, nameof(ApiKeyValidationMiddleware), ex);
            throw;
        }
    }
}
