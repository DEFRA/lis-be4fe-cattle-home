// <copyright file="RequestValidationMiddlewareTest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.Middleware;

using System.Text.Json;
using Defra.Lis.Be4Fe.Api.MetaData;
using Defra.Lis.Be4Fe.Api.Middleware;
using Defra.Lis.Be4Fe.Api.Middleware.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

public sealed class RequestValidationMiddlewareTest
{
    [Fact]
    public async Task ApiKeyValidationShouldContinueForAnUnmatchedRoute()
    {
        var middleware = new ApiKeyValidationMiddleware("secret", NullLogger<ApiKeyValidationMiddleware>.Instance);
        var context = CreateContext();
        var nextCalled = false;

        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task ApiKeyValidationShouldContinueWhenCheckIsIgnored()
    {
        var middleware = new ApiKeyValidationMiddleware("secret", NullLogger<ApiKeyValidationMiddleware>.Instance);
        var context = CreateContext(new IgnoreApiKeyCheck());
        var nextCalled = false;

        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null, "missing_header")]
    [InlineData("wrong", "invalid_api_key")]
    public async Task ApiKeyValidationShouldRejectInvalidHeaders(string? apiKey, string expectedCode)
    {
        var middleware = new ApiKeyValidationMiddleware("secret", NullLogger<ApiKeyValidationMiddleware>.Instance);
        var context = CreateContext(new object());
        context.Request.Path = "/api/cattle";
        if (apiKey is not null)
        {
            context.Request.Headers[RequestHeaderNames.ApiKey] = apiKey;
        }

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        context.Response.ContentType.ShouldBe("application/json");
        var payload = await ReadResponseAsync(context);
        payload.GetProperty("error").GetProperty("code").GetString().ShouldBe(expectedCode);
        payload.GetProperty("error").GetProperty("path").GetString().ShouldBe("/api/cattle");
    }

    [Fact]
    public async Task ApiKeyValidationShouldContinueForAValidHeader()
    {
        var middleware = new ApiKeyValidationMiddleware("secret", NullLogger<ApiKeyValidationMiddleware>.Instance);
        var context = CreateContext(new object());
        context.Request.Headers[RequestHeaderNames.ApiKey] = "secret";
        var nextCalled = false;

        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task ApiKeyValidationShouldRejectAnEmptyConfiguredKey()
    {
        var middleware = new ApiKeyValidationMiddleware(string.Empty, NullLogger<ApiKeyValidationMiddleware>.Instance);

        await Should.ThrowAsync<ArgumentException>(() => middleware.InvokeAsync(CreateContext(), _ => Task.CompletedTask));
    }

    [Fact]
    public async Task CorrelationIdValidationShouldContinueWhenCheckIsIgnored()
    {
        var middleware = new CorrelationIdMiddleware(NullLogger<CorrelationIdMiddleware>.Instance);
        var context = CreateContext(new IgnoreCorrelationIdCheck());
        var nextCalled = false;

        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\"\"")]
    [InlineData("'  '")]
    public async Task CorrelationIdValidationShouldRejectMissingValues(string? correlationId)
    {
        var middleware = new CorrelationIdMiddleware(NullLogger<CorrelationIdMiddleware>.Instance);
        var context = CreateContext(new object());
        if (correlationId is not null)
        {
            context.Request.Headers[RequestHeaderNames.CorrelationId] = correlationId;
        }

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        var payload = await ReadResponseAsync(context);
        payload.GetProperty("error").GetProperty("code").GetString().ShouldBe("missing_header");
    }

    [Fact]
    public async Task CorrelationIdValidationShouldContinueForAValueSurroundedByWhitespace()
    {
        var middleware = new CorrelationIdMiddleware(NullLogger<CorrelationIdMiddleware>.Instance);
        var context = CreateContext(new object());
        context.Request.Headers[RequestHeaderNames.CorrelationId] = "  correlation-id  ";
        var nextCalled = false;

        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidationMiddlewareShouldPropagateDownstreamExceptions()
    {
        var middleware = new CorrelationIdMiddleware(NullLogger<CorrelationIdMiddleware>.Instance);
        var context = CreateContext(new object());
        context.Request.Headers[RequestHeaderNames.CorrelationId] = "correlation-id";

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(context, _ => throw new InvalidOperationException("failure")));

        exception.Message.ShouldBe("failure");
    }

    private static DefaultHttpContext CreateContext(params object[] metadata)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        if (metadata.Length > 0)
        {
            context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(metadata), "test"));
        }

        return context;
    }

    private static async Task<JsonElement> ReadResponseAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body, cancellationToken: TestContext.Current.CancellationToken);
        return document.RootElement.Clone();
    }
}
