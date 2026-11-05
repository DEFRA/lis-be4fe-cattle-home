// <copyright file="LoggableRequestPathEnricherTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.Logging;

using Defra.Lis.Be4Fe.Api.Utils.Logging;
using Microsoft.AspNetCore.Http;
using Serilog.Events;
using Serilog.Parsing;

public class LoggableRequestPathEnricherTests
{
    private const string RawPath = "/api/users/5e0c8f8a-4b1d-4c7e-9a3f-6d2b8e1f0a47";

    [Fact]
    public void Enrich_RemovesTheRawPath_WhenNoRequestIsInProgress()
    {
        // Work that outlives its request, such as a circuit breaker callback, still carries the request's log scope.
        var enricher = new LoggableRequestPathEnricher(new HttpContextAccessor());
        var logEvent = EventWithRequestPath(RawPath);

        enricher.Enrich(logEvent, new PropertyFactory());

        logEvent.Properties.ShouldNotContainKey(LoggableRequestPathEnricher.PropertyName);
    }

    [Fact]
    public void Enrich_ReplacesTheRawPathWithTheUnmatchedMarker_WhenTheRequestHasNoRoute()
    {
        var enricher = new LoggableRequestPathEnricher(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        var logEvent = EventWithRequestPath(RawPath);

        enricher.Enrich(logEvent, new PropertyFactory());

        ((ScalarValue)logEvent.Properties[LoggableRequestPathEnricher.PropertyName]).Value.ShouldBe(LoggableRequestPath.UnmatchedRoute);
    }

    private static LogEvent EventWithRequestPath(string path) => new(
        DateTimeOffset.UtcNow,
        LogEventLevel.Warning,
        null,
        new MessageTemplateParser().Parse("The cattle API circuit opened."),
        [new LogEventProperty(LoggableRequestPathEnricher.PropertyName, new ScalarValue(path))]);

    private sealed class PropertyFactory : Serilog.Core.ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false) =>
            new(name, new ScalarValue(value));
    }
}
