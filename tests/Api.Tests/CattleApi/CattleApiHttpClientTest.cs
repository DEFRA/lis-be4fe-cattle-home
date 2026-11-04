// <copyright file="CattleApiHttpClientTest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.CattleApi;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Defra.Lis.Be4Fe.Api.Utils.Http;
using Defra.Lis.Be4Fe.CattleApi;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Rest.Client;
using Defra.Livestock.Sdk.Api.Strategies.Operations;
using Microsoft.AspNetCore.HeaderPropagation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Polly.CircuitBreaker;
using Polly.Timeout;

/// <summary>
/// Runs the cattle API client over the HTTP client <see cref="CattleApiHttpClient"/> registers, as Program does, to
/// prove the timeout and circuit breaker (LREG-508): a hung or unreachable cattle API becomes a 503 within the budget,
/// repeated failures open the circuit so calls fail fast, and a successful probe closes it again.
/// </summary>
public sealed class CattleApiHttpClientTest : IDisposable
{
    private const string Cph = "22/001/0001";
    private const string CorrelationId = "corr-resilience";
    private const string Holding = """{"cph":"22/001/0001","name":"Oakfield Farm","active":true}""";

    // Rows are keyed by name, not by exception instance, so xUnit can serialise and list each one (xUnit1045).
    private static readonly Dictionary<string, Func<Exception>> Failures = new()
    {
        [nameof(HttpRequestException)] = () => new HttpRequestException("Connection refused (cattle-api.test:80)"),
        [nameof(TimeoutRejectedException)] = () => new TimeoutRejectedException(),
        [nameof(OperationCanceledException)] = () => new OperationCanceledException(),
        [nameof(BrokenCircuitException)] = () => new BrokenCircuitException(),
        [nameof(InvalidOperationException)] = () => new InvalidOperationException(),
    };

    private readonly StubHttpMessageHandler handler = new();
    private readonly CapturingLoggerProvider logs = new();
    private ServiceProvider? provider;

    public static TheoryData<string> FailuresWithoutResponse =>
        [nameof(HttpRequestException), nameof(TimeoutRejectedException)];

    public static TheoryData<string> FailuresThatDoNotCount =>
        [nameof(OperationCanceledException), nameof(BrokenCircuitException), nameof(InvalidOperationException)];

    public void Dispose() => provider?.Dispose();

    [Fact]
    public async Task HungCattleApiShouldTimeOutWithinTheBudgetAsServiceUnavailable()
    {
        handler.Hang();
        var client = CreateClient(new CattleApiOptions { TimeoutSeconds = 1 });
        var stopwatch = Stopwatch.StartNew();

        var exception = await Should.ThrowAsync<ServiceUnavailableException>(
            () => client.GetHoldingDetailsAsync(Cph, TestContext.Current.CancellationToken));

        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1) + CattleApiHttpClient.BodyGrace);
        exception.InnerException.ShouldBeOfType<RestRequestException>().InnerException.ShouldBeOfType<TimeoutRejectedException>();
    }

    [Fact]
    public void HttpClientTimeoutShouldAllowTheBodyGraceAfterThePipelineTimeout()
    {
        CreateClient(new CattleApiOptions { TimeoutSeconds = 3 });

        // A typed client is named after its service type.
        var httpClient = provider!.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(IRestHttpClient));

        httpClient.Timeout.ShouldBe(TimeSpan.FromSeconds(3) + CattleApiHttpClient.BodyGrace);
    }

    [Fact]
    public async Task RepeatedTransportFailuresShouldOpenTheCircuitAndFailFast()
    {
        handler.FailWith(new HttpRequestException("Connection refused"))
            .FailWith(new HttpRequestException("Connection refused"));
        var client = CreateClient(new CattleApiOptions { CircuitBreakerFailureThreshold = 2 });

        await Should.ThrowAsync<ServiceUnavailableException>(() => client.GetHoldingDetailsAsync(Cph, TestContext.Current.CancellationToken));
        await Should.ThrowAsync<ServiceUnavailableException>(() => client.GetHoldingDetailsAsync(Cph, TestContext.Current.CancellationToken));
        var exception = await Should.ThrowAsync<ServiceUnavailableException>(
            () => client.GetHoldingDetailsAsync(Cph, TestContext.Current.CancellationToken));

        exception.InnerException.ShouldBeOfType<RestRequestException>().InnerException.ShouldBeAssignableTo<BrokenCircuitException>();
        handler.Requests.Count.ShouldBe(2);
        await LoggedAsync(LogLevel.Warning, "circuit opened: at least 2 calls");
    }

    [Fact]
    public async Task SuccessfulProbeAfterTheBreakShouldCloseTheCircuit()
    {
        handler.FailWith(new HttpRequestException("Connection refused"))
            .FailWith(new HttpRequestException("Connection refused"))
            .RespondWith(HttpStatusCode.OK, Holding)
            .RespondWith(HttpStatusCode.OK, Holding);
        var client = CreateClient(new CattleApiOptions { CircuitBreakerFailureThreshold = 2, CircuitBreakerBreakSeconds = 1 });

        await Should.ThrowAsync<ServiceUnavailableException>(() => client.GetHoldingDetailsAsync(Cph, TestContext.Current.CancellationToken));
        await Should.ThrowAsync<ServiceUnavailableException>(() => client.GetHoldingDetailsAsync(Cph, TestContext.Current.CancellationToken));
        await Task.Delay(TimeSpan.FromSeconds(1.2), TestContext.Current.CancellationToken);

        (await client.GetHoldingDetailsAsync(Cph, TestContext.Current.CancellationToken)).Cph.ShouldBe(Cph);
        (await client.GetHoldingDetailsAsync(Cph, TestContext.Current.CancellationToken)).Cph.ShouldBe(Cph);

        handler.Requests.Count.ShouldBe(4);
        await LoggedAsync(LogLevel.Information, "half-open");
        await LoggedAsync(LogLevel.Information, "circuit closed");
    }

    [Fact]
    public async Task FailedProbeAfterTheBreakShouldReopenTheCircuit()
    {
        handler.FailWith(new HttpRequestException("Connection refused"))
            .FailWith(new HttpRequestException("Connection refused"))
            .FailWith(new HttpRequestException("Connection refused"));
        var client = CreateClient(new CattleApiOptions { CircuitBreakerFailureThreshold = 2, CircuitBreakerBreakSeconds = 1 });

        await Should.ThrowAsync<ServiceUnavailableException>(() => client.GetHoldingDetailsAsync(Cph, TestContext.Current.CancellationToken));
        await Should.ThrowAsync<ServiceUnavailableException>(() => client.GetHoldingDetailsAsync(Cph, TestContext.Current.CancellationToken));
        await Task.Delay(TimeSpan.FromSeconds(1.2), TestContext.Current.CancellationToken);

        await Should.ThrowAsync<ServiceUnavailableException>(() => client.GetHoldingDetailsAsync(Cph, TestContext.Current.CancellationToken));
        var exception = await Should.ThrowAsync<ServiceUnavailableException>(
            () => client.GetHoldingDetailsAsync(Cph, TestContext.Current.CancellationToken));

        exception.InnerException.ShouldBeOfType<RestRequestException>().InnerException.ShouldBeAssignableTo<BrokenCircuitException>();
        handler.Requests.Count.ShouldBe(3);
        await LoggedAsync(LogLevel.Warning, "circuit reopened: the probe call failed");
        logs.Messages.Count(m => m.Text.Contains("circuit opened", StringComparison.Ordinal)).ShouldBe(1);
    }

    [Fact]
    public async Task ServerErrorResponsesShouldNotOpenTheCircuit()
    {
        // Any response proves the cattle API is up; a 500 stays a 500 and the next call still reaches it.
        handler.RespondWith(HttpStatusCode.InternalServerError, """{"status":500}""")
            .RespondWith(HttpStatusCode.InternalServerError, """{"status":500}""")
            .RespondWith(HttpStatusCode.OK, Holding);
        var client = CreateClient(new CattleApiOptions { CircuitBreakerFailureThreshold = 2 });

        await Should.ThrowAsync<RestResponseException>(() => client.GetHoldingDetailsAsync(Cph, TestContext.Current.CancellationToken));
        await Should.ThrowAsync<RestResponseException>(() => client.GetHoldingDetailsAsync(Cph, TestContext.Current.CancellationToken));

        (await client.GetHoldingDetailsAsync(Cph, TestContext.Current.CancellationToken)).Cph.ShouldBe(Cph);
        handler.Requests.Count.ShouldBe(3);
    }

    [Fact]
    public async Task CallerCancellationShouldNotOpenTheCircuit()
    {
        // Each call is cancelled once its request reaches the cattle API, so the cancellation never races the request.
        using var first = new CancellationTokenSource();
        using var second = new CancellationTokenSource();
        handler.Hang(first.Cancel).Hang(second.Cancel).RespondWith(HttpStatusCode.OK, Holding);
        var client = CreateClient(new CattleApiOptions { CircuitBreakerFailureThreshold = 2 });

        foreach (var cancellation in new[] { first, second })
        {
            var exception = await Should.ThrowAsync<Exception>(() => client.GetHoldingDetailsAsync(Cph, cancellation.Token));
            exception.ShouldNotBeOfType<ServiceUnavailableException>();
        }

        (await client.GetHoldingDetailsAsync(Cph, TestContext.Current.CancellationToken)).Cph.ShouldBe(Cph);
        handler.Requests.Count.ShouldBe(3);
    }

    [Fact]
    public async Task CorrelationIdShouldStillReachTheCattleApiThroughThePipeline()
    {
        handler.RespondWith(HttpStatusCode.OK, Holding);
        var client = CreateClient(new CattleApiOptions());

        await client.GetHoldingDetailsAsync(Cph, TestContext.Current.CancellationToken);

        handler.Requests.ShouldHaveSingleItem().Headers.GetValues("x-cdp-request-id").ShouldBe([CorrelationId]);
    }

    [Theory]
    [MemberData(nameof(FailuresWithoutResponse))]
    public void FailuresWithoutAResponseShouldCountTowardsOpeningTheCircuit(string failure) =>
        CattleApiHttpClient.IsFailureWithoutResponse(Polly.Outcome.FromException<HttpResponseMessage>(Failures[failure]())).ShouldBeTrue();

    [Theory]
    [MemberData(nameof(FailuresThatDoNotCount))]
    public void OtherFailuresShouldNotCountTowardsOpeningTheCircuit(string failure) =>
        CattleApiHttpClient.IsFailureWithoutResponse(Polly.Outcome.FromException<HttpResponseMessage>(Failures[failure]())).ShouldBeFalse();

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public void ResponsesShouldNotCountTowardsOpeningTheCircuit(HttpStatusCode statusCode)
    {
        using var response = new HttpResponseMessage(statusCode);

        CattleApiHttpClient.IsFailureWithoutResponse(Polly.Outcome.FromResult(response)).ShouldBeFalse();
    }

    // Polly runs the circuit callbacks off the request path, so a transition log can land just after the call returns.
    private async Task LoggedAsync(LogLevel level, string text)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);

        while (!logs.Messages.Any(m => m.Level == level && m.Text.Contains(text, StringComparison.Ordinal)) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }

        logs.Messages.ShouldContain(m => m.Level == level && m.Text.Contains(text, StringComparison.Ordinal));
    }

    private CattleHoldingRestClient CreateClient(CattleApiOptions overrides)
    {
        var options = new CattleApiOptions
        {
            BaseUrl = "http://cattle-api.test/",
            ApiKey = "key",
            TimeoutSeconds = overrides.TimeoutSeconds,
            CircuitBreakerFailureThreshold = overrides.CircuitBreakerFailureThreshold,
            CircuitBreakerSamplingSeconds = overrides.CircuitBreakerSamplingSeconds,
            CircuitBreakerBreakSeconds = overrides.CircuitBreakerBreakSeconds,
        };

        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddProvider(logs));
        services.AddSingleton(options);
        services.AddHeaderPropagation(propagation => propagation.Headers.Add("x-cdp-request-id"));
        services.AddCattleApiHttpClient().ConfigurePrimaryHttpMessageHandler(() => handler);
        provider = services.BuildServiceProvider();

        // Header propagation reads the inbound headers the middleware captured for this request.
        provider.GetRequiredService<HeaderPropagationValues>().Headers =
            new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase) { ["x-cdp-request-id"] = CorrelationId };

        var factory = new RestStrategyFactory<CattleHoldingRestClient>(provider);

        return new CattleHoldingRestClient(factory, options, NullLogger<CattleHoldingRestClient>.Instance);
    }

    private sealed record LogMessage(LogLevel Level, string Text);

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<LogMessage> Messages { get; } = new();

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Messages);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(ConcurrentQueue<LogMessage> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
                messages.Enqueue(new LogMessage(logLevel, formatter(state, exception)));
        }
    }
}
