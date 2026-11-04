// <copyright file="LoggableRequestPathEnricher.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Utils.Logging;

using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

/// <summary>
/// Replaces the <c>RequestPath</c> property that the ASP.NET Core hosting log scope adds to every event in a request
/// with the route template from <see cref="LoggableRequestPath"/>, so the raw path never reaches the logs.
/// </summary>
/// <param name="httpContextAccessor">Gives access to the current request.</param>
public sealed class LoggableRequestPathEnricher(IHttpContextAccessor httpContextAccessor) : ILogEventEnricher
{
    /// <summary>The property the hosting log scope and Serilog request logging write the path to.</summary>
    public const string PropertyName = "RequestPath";

    /// <inheritdoc />
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(propertyFactory);

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            // No request is in progress, yet work started inside one (such as the cattle API circuit breaker's
            // callbacks, which Polly runs on its own task) still carries that request's hosting scope and its raw path.
            logEvent.RemovePropertyIfPresent(PropertyName);
            return;
        }

        // Overwrites the scope's value whichever runs first: the scope only adds the property when it is absent.
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(PropertyName, LoggableRequestPath.For(httpContext)));
    }
}
