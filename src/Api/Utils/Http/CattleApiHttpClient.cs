// <copyright file="CattleApiHttpClient.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Utils.Http;

using Defra.Lis.Be4Fe.CattleApi;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Rest.Client;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Rest.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

/// <summary>
/// Registers the strategies SDK's HTTP client, which carries every call to the cattle API, with a timeout and a
/// circuit breaker (LREG-508), so a slow or unreachable cattle API fails fast instead of holding the request.
/// </summary>
/// <remarks>
/// <para>
/// The pipeline times out a call that has no response headers after <see cref="CattleApiOptions.TimeoutSeconds"/>.
/// The HttpClient timeout, one second later, cuts off a response body that stalls after the headers. Both surface as
/// a <c>RestRequestException</c>, which <see cref="CattleHoldingRestClient"/> turns into a 503.
/// </para>
/// <para>
/// The circuit opens when every call in the sampling window, and at least
/// <see cref="CattleApiOptions.CircuitBreakerFailureThreshold"/> of them, failed without a response: a transport failure
/// or a timeout. Any HTTP response, even a 5xx, proves the cattle API is up and does not count, as in the hub's breaker
/// (lis-hubs-infra-core). While open, calls fail at once with a <see cref="BrokenCircuitException"/>. After
/// <see cref="CattleApiOptions.CircuitBreakerBreakSeconds"/> one probe is let through; its outcome closes or reopens it.
/// There is no retry: the cattle-home app decides whether to retry, and retrying at several layers multiplies the load.
/// </para>
/// </remarks>
public static partial class CattleApiHttpClient
{
    /// <summary>
    /// The resilience pipeline's name, which also prefixes Polly's own telemetry.
    /// </summary>
    public const string PipelineName = "cattle-api";

    /// <summary>
    /// The extra time the HttpClient timeout allows a response body after the pipeline timeout for the headers.
    /// </summary>
    public static readonly TimeSpan BodyGrace = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Registers the SDK's <see cref="IRestHttpClient"/> with header propagation, the timeout and the circuit breaker.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The client builder, so tests can replace the primary handler.</returns>
    public static IHttpClientBuilder AddCattleApiHttpClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Header propagation is registered first so x-cdp-request-id reaches the cattle API on every call.
        // The HttpClient factory's own loggers are removed: their "HTTP GET {Uri}" scope wraps the pipeline, so every
        // event logged inside it (Polly's telemetry, the circuit transitions) would carry the raw URL, which can hold the
        // Defra ID subject (LREG-751). CdpLogging already mutes those loggers below Warning, so nothing else is lost.
        var builder = services.AddHttpClient<IRestHttpClient, RestHttpClient>()
            .RemoveAllLoggers()
            .AddHeaderPropagation()
            .ConfigureHttpClient((provider, client) =>
                client.Timeout = TimeSpan.FromSeconds(provider.GetRequiredService<CattleApiOptions>().TimeoutSeconds) + BodyGrace);

        builder.AddResilienceHandler(PipelineName, (pipeline, context) =>
        {
            var options = context.ServiceProvider.GetRequiredService<CattleApiOptions>();
            var logger = context.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(CattleApiHttpClient));

            // 1 while the half-open probe is in flight, so a failed probe is logged as a reopen. One per pipeline.
            var probing = 0;

            // The breaker is outside the timeout, so it sees the timeout as a failure.
            pipeline.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 1.0,
                MinimumThroughput = options.CircuitBreakerFailureThreshold,
                SamplingDuration = TimeSpan.FromSeconds(options.CircuitBreakerSamplingSeconds),
                BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakerBreakSeconds),
                ShouldHandle = args => ValueTask.FromResult(IsFailureWithoutResponse(args.Outcome)),
                OnOpened = args =>
                {
                    // Polly opens the circuit again, through the same callback, when the half-open probe fails.
                    var reopened = Interlocked.Exchange(ref probing, 0) == 1;

                    return OutsideAnyRequest(() =>
                    {
                        if (reopened)
                        {
                            LogCircuitReopened(logger, args.BreakDuration.TotalSeconds);
                        }
                        else
                        {
                            LogCircuitOpened(logger, options.CircuitBreakerFailureThreshold, args.BreakDuration.TotalSeconds);
                        }
                    });
                },
                OnHalfOpened = _ =>
                {
                    Interlocked.Exchange(ref probing, 1);
                    return OutsideAnyRequest(() => LogCircuitHalfOpened(logger));
                },
                OnClosed = _ =>
                {
                    Interlocked.Exchange(ref probing, 0);
                    return OutsideAnyRequest(() => LogCircuitClosed(logger));
                },
            });

            pipeline.AddTimeout(TimeSpan.FromSeconds(options.TimeoutSeconds));
        });

        return builder;
    }

    /// <summary>
    /// Whether a call failed without the cattle API answering: a transport failure (connection refused, DNS failure) or
    /// the pipeline timeout. The caller cancelling is not a failure of the cattle API, so it is not counted.
    /// </summary>
    /// <param name="outcome">The outcome of one call through the pipeline.</param>
    /// <returns><see langword="true"/> when the call counts towards opening the circuit.</returns>
    public static bool IsFailureWithoutResponse(Outcome<HttpResponseMessage> outcome) =>
        outcome.Exception is HttpRequestException or TimeoutRejectedException;

    /// <summary>
    /// Runs a circuit transition log on a task that carries no request context. Polly runs the breaker's callbacks on
    /// its own task, which captured the execution context of the request that first used the pipeline, so without this
    /// every transition would be logged with that request's correlation ID and log scope, long after it ended.
    /// </summary>
    /// <param name="log">The log call.</param>
    /// <returns>A task that completes once the log call has run.</returns>
    private static ValueTask OutsideAnyRequest(Action log)
    {
        using (ExecutionContext.SuppressFlow())
        {
            return new ValueTask(Task.Run(log));
        }
    }
}
