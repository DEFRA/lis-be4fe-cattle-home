// <copyright file="SubjectLoggingTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.Logging;

using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Defra.Lis.Be4Fe.Api.Endpoints.Users;
using Defra.Lis.Be4Fe.Api.Exceptions;
using Defra.Lis.Be4Fe.Api.Middleware;
using Defra.Lis.Be4Fe.Api.Middleware.Headers;
using Defra.Lis.Be4Fe.Api.Services;
using Defra.Lis.Be4Fe.Api.Tests.Authentication;
using Defra.Lis.Be4Fe.Api.Tests.CattleApi;
using Defra.Lis.Be4Fe.Api.Utils.Logging;
using Defra.Lis.Be4Fe.CattleApi;
using Defra.Livestock.Sdk.Api.Strategies;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Rest.Client;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Rest.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;

/// <summary>
/// Runs the user endpoints through the logging pipeline from <c>Program</c>, against the real cattle API client over a
/// stub, and checks that no log event carries the Defra ID subject.
/// </summary>
public class SubjectLoggingTests
{
    private const string Subject = "5e0c8f8a-4b1d-4c7e-9a3f-6d2b8e1f0a47";
    private const string CorrelationId = "corr-subject";
    private const string CattleApiBaseUrl = "http://cattle-api.test/";

    private const string UserDetails = $$"""
                                       {
                                         "subject": "{{Subject}}",
                                         "email": "oakfield.farmer@oakhill-farms.co.uk",
                                         "cphs": [{ "cph": "22/001/0001", "role": "Keeper" }]
                                       }
                                       """;

    private readonly StubHttpMessageHandler cattleApi = new();
    private readonly CapturingSink sink = new();

    public static TheoryData<HttpStatusCode, HttpStatusCode> GetUserOutcomes => new()
    {
        { HttpStatusCode.OK, HttpStatusCode.OK },
        { HttpStatusCode.NotFound, HttpStatusCode.NotFound },
        { HttpStatusCode.BadRequest, HttpStatusCode.BadRequest },
        { HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable },
    };

    public static TheoryData<HttpStatusCode, HttpStatusCode> CreateUserOutcomes => new()
    {
        { HttpStatusCode.OK, HttpStatusCode.OK },
        { HttpStatusCode.BadRequest, HttpStatusCode.BadRequest },
        { HttpStatusCode.Conflict, HttpStatusCode.Conflict },
        { HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable },
    };

    [Theory]
    [MemberData(nameof(GetUserOutcomes))]
    public async Task GetUserDetails_LogsNoSubject(HttpStatusCode cattleApiStatus, HttpStatusCode expectedStatus)
    {
        RespondWith(cattleApiStatus);
        await using var app = await StartAppAsync();

        var response = await AuthenticatedClient(app).GetAsync($"/api/users/{Subject}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(expectedStatus);
        AssertRequestLoggedWithRouteTemplate("GET", "/api/users/{userId}");
        AssertNoEventContainsTheSubject();
    }

    [Theory]
    [MemberData(nameof(GetUserOutcomes))]
    public async Task GetCphsForUser_LogsNoSubject(HttpStatusCode cattleApiStatus, HttpStatusCode expectedStatus)
    {
        RespondWith(cattleApiStatus);
        await using var app = await StartAppAsync();

        var response = await AuthenticatedClient(app).GetAsync($"/api/users/{Subject}/cphs", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(expectedStatus);
        AssertRequestLoggedWithRouteTemplate("GET", "/api/users/{userId}/cphs");
        AssertNoEventContainsTheSubject();
    }

    [Theory]
    [MemberData(nameof(CreateUserOutcomes))]
    public async Task CreateUser_LogsNoSubject(HttpStatusCode cattleApiStatus, HttpStatusCode expectedStatus)
    {
        RespondWith(cattleApiStatus);
        await using var app = await StartAppAsync();

        var response = await AuthenticatedClient(app).PostAsync("/api/users", ClaimsBody(), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(expectedStatus);
        AssertNoEventContainsTheSubject();
    }

    [Fact]
    public async Task GetUserDetails_LogsNoSubject_WhenTheCorrelationIdIsMissing()
    {
        await using var app = await StartAppAsync();

        var response = await app.GetAuthenticatedTestClient().GetAsync($"/api/users/{Subject}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        cattleApi.Requests.ShouldBeEmpty();
        sink.Events.ShouldContain(e => e.MessageTemplate.Text.Contains(RequestHeaderNames.CorrelationId, StringComparison.Ordinal));
        AssertNoEventContainsTheSubject();
    }

    [Fact]
    public async Task GetUserDetails_LogsNoSubject_WhenTheApiKeyIsMissing()
    {
        await using var app = await StartAppAsync();
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add(RequestHeaderNames.CorrelationId, CorrelationId);

        var response = await client.GetAsync($"/api/users/{Subject}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        sink.Events.ShouldContain(e => e.MessageTemplate.Text.Contains("API key", StringComparison.Ordinal));
        AssertNoEventContainsTheSubject();
    }

    [Fact]
    public async Task UnmatchedRoute_LogsNoRawPath()
    {
        await using var app = await StartAppAsync();

        var response = await AuthenticatedClient(app).GetAsync($"/api/user/{Subject}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        AssertRequestLoggedWithRouteTemplate("GET", LoggableRequestPath.UnmatchedRoute);
        AssertNoEventContainsTheSubject();
    }

    private static StringContent ClaimsBody() => new(
        $$"""{"sub":"{{Subject}}","email":"oakfield.farmer@oakhill-farms.co.uk","given_name":"Oakfield","family_name":"Farmer"}""",
        Encoding.UTF8,
        "application/json");

    private static HttpClient AuthenticatedClient(WebApplication app)
    {
        var client = app.GetAuthenticatedTestClient();
        client.DefaultRequestHeaders.Add(RequestHeaderNames.CorrelationId, CorrelationId);
        return client;
    }

    private static string Describe(LogEvent logEvent)
    {
        var text = new StringBuilder(logEvent.RenderMessage());
        foreach (var property in logEvent.Properties)
        {
            text.Append(' ').Append(property.Key).Append('=').Append(property.Value);
        }

        return text.Append(' ').Append(logEvent.Exception).ToString();
    }

    private void RespondWith(HttpStatusCode status) =>
        cattleApi.RespondWith(status, status == HttpStatusCode.OK ? UserDetails : """{"status":400}""");

    private void AssertRequestLoggedWithRouteTemplate(string method, string template)
    {
        var request = sink.Events.Where(e => e.Properties.ContainsKey("Elapsed")).ShouldHaveSingleItem();
        ((ScalarValue)request.Properties["RequestMethod"]).Value.ShouldBe(method);
        ((ScalarValue)request.Properties["RequestPath"]).Value.ShouldBe(template);
    }

    private void AssertNoEventContainsTheSubject()
    {
        sink.Events.ShouldNotBeEmpty();
        sink.Events.ShouldAllBe(e => !Describe(e).Contains(Subject, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<WebApplication> StartAppAsync()
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.WebHost.UseTestServer();

        // Information and above, as in production, but without the overrides from appsettings, and everything from
        // this service. The overrides CdpLogging enforces in code still apply. The static Log.Logger is left alone, as
        // tests running in parallel would otherwise write to each other's sinks.
        builder.Services.AddHttpContextAccessor();
        builder.Host.UseSerilog(
            (context, services, configuration) =>
            {
                CdpLogging.Configuration(context, services, configuration);
                configuration.MinimumLevel.Information()
                    .MinimumLevel.Override("Defra", LogEventLevel.Verbose)
                    .WriteTo.Sink(sink);
            },
            preserveStaticLogger: true);

        builder.Services.AddRouting();
        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);
        builder.Services.AddTestServiceToServiceAuthentication();
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();
        builder.Services.AddTransient<CorrelationIdMiddleware>();

        // The cattle API client is registered as in Program, so the SDK and HttpClient factory loggers run too.
        builder.Services.AddStrategyFramework();
        builder.Services.AddHttpClient<IRestHttpClient, RestHttpClient>().ConfigurePrimaryHttpMessageHandler(() => cattleApi);
        builder.Services.AddRestStrategyFactory<CattleHoldingRestClient>();
        builder.Services.AddSingleton(new CattleApiOptions { BaseUrl = CattleApiBaseUrl, ApiKey = nameof(SubjectLoggingTests) });
        builder.Services.AddScoped<ICattleHoldingClient, CattleHoldingRestClient>();
        builder.Services.AddScoped<IUserLookupService, UserLookupService>();

        var app = builder.Build();
        app.UseCdpRequestLogging();
        app.UseExceptionHandler();
        app.UseRouting();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseTestServiceToServiceAuthentication();
        app.UseUserEndpoints();
        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    private sealed class CapturingSink : ILogEventSink
    {
        private readonly ConcurrentQueue<LogEvent> events = new();

        public IReadOnlyCollection<LogEvent> Events => events;

        public void Emit(LogEvent logEvent) => events.Enqueue(logEvent);
    }
}
