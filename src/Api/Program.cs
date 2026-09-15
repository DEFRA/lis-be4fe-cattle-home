// <copyright file="Program.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Defra.Lis.Be4Fe.Api.Config;
using Defra.Lis.Be4Fe.Api.Endpoints;
using Defra.Lis.Be4Fe.Api.Endpoints.Cattle;
using Defra.Lis.Be4Fe.Api.Endpoints.Cphs;
using Defra.Lis.Be4Fe.Api.Endpoints.Health;
using Defra.Lis.Be4Fe.Api.Endpoints.Users;
using Defra.Lis.Be4Fe.Api.Exceptions;
using Defra.Lis.Be4Fe.Api.Foundation.Caching;
using Defra.Lis.Be4Fe.Api.Services;
using Defra.Lis.Be4Fe.Api.Utils;
using Defra.Lis.Be4Fe.Api.Utils.Http;
using Defra.Lis.Be4Fe.Api.Utils.Logging;
using Defra.Lis.Be4Fe.Api.Utils.Mongo;
using Defra.Lis.Be4Fe.CattleApi;
using Defra.Livestock.Sdk.Api.Strategies;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Rest.Client;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Rest.Client;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;

public class Program
{
    protected Program()
    {
    }

    public static async Task Main(string[] args)
    {
        var app = CreateWebApplication(args);
        await app.RunAsync();
    }

    [ExcludeFromCodeCoverage]
    private static WebApplication CreateWebApplication(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile(
                $"appsettings.{builder.Environment.EnvironmentName}.json",
                optional: true,
                reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args);

        ConfigureBuilder(builder, builder.Configuration);

        var app = builder.Build();
        return SetupApplication(app);
    }

    [ExcludeFromCodeCoverage]
    private static void ConfigureBuilder(
        WebApplicationBuilder builder,
        IConfigurationRoot configuration)
    {
        // Configure logging to use the CDP Platform standards.
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHealthChecks();
        builder.Host.UseSerilog(CdpLogging.Configuration);
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();
        builder.Services.AddStrategyFramework();
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
        });

        // Default HTTP Client
        builder.Services
            .AddHttpClient("DefaultClient")
            .AddHeaderPropagation();

        // Proxy HTTP Client
        builder.Services.AddTransient<ProxyHttpMessageHandler>();
        builder.Services
            .AddHttpClient("proxy")
            .ConfigurePrimaryHttpMessageHandler<ProxyHttpMessageHandler>();

        // Propagate trace header.
        builder.Services.AddHeaderPropagation(options =>
        {
            var traceHeader = builder.Configuration.GetValue<string>("TraceHeader");
            if (!string.IsNullOrWhiteSpace(traceHeader))
            {
                options.Headers.Add(traceHeader);
            }
        });

        var services = builder.Services;

        // Trust material must be loaded before anything creates outbound connections.
        services.LoadCustomTrustStoreFromEnvironment();

        services.AddProblemDetails();
        services.AddValidation();

        services.AddHttpContextAccessor();

        ConfigureHeaderPropagation(services, configuration);
        ConfigureExternalDependencies(services, configuration);
        ConfigureMongo(services, configuration);
        ConfigureOpenApi(services);

        services.AddHealthChecks();
    }

    [ExcludeFromCodeCoverage]
    private static void ConfigureHeaderPropagation(IServiceCollection services, IConfiguration configuration)
    {
        var traceHeader = configuration.GetValue<string>("TraceHeader");

        services.AddHeaderPropagation(options =>
        {
            if (!string.IsNullOrWhiteSpace(traceHeader))
            {
                options.Headers.Add(traceHeader);
            }
        });
    }

    [ExcludeFromCodeCoverage]
    private static void ConfigureExternalDependencies(IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<ProxyHttpMessageHandler>();
        services.AddSingleton(TimeProvider.System);

        services.AddOptions<CacheOptions>()
            .Bind(configuration.GetRequiredSection(CacheOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Cattle API options are validated when first used, so the service boots (and answers /health)
        // before the base URL and API key are configured.
        services.AddOptions<CattleApiOptions>()
            .Bind(configuration.GetRequiredSection(CattleApiOptions.SectionName));
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<CattleApiOptions>>().Value);

        // Strategies SDK REST client, registered before the factory so it carries header propagation
        // (x-cdp-request-id) to the cattle API.
        services.AddHttpClient<IRestHttpClient, RestHttpClient>().AddHeaderPropagation();
        services.AddRestStrategyFactory<CattleHoldingRestClient>();
        services.AddScoped<ICattleHoldingClient, CattleHoldingRestClient>();

        services.AddSingleton<IExternalDataCacheRepository, MongoExternalDataCacheRepository>();
        services.AddSingleton<ICachedDataService, CachedDataService>();
        services.AddSingleton<ICattleApiClient>(_ =>
        {
            var fixturePath = configuration.GetValue<string>($"{CattleApiOptions.SectionName}:FixturePath")
                ?? throw new InvalidOperationException("CattleApi:FixturePath must be configured.");
            var resolvedFixturePath = Path.IsPathRooted(fixturePath)
                ? fixturePath
                : Path.Combine(AppContext.BaseDirectory, fixturePath);

            return new JsonCattleApiClient(resolvedFixturePath);
        });
        services.AddSingleton<IUserLookupService, UserLookupService>();
        services.AddScoped<ICphLookupService, CphLookupService>();
        services.AddScoped<IHoldingLookupService, HoldingLookupService>();
        services.AddSingleton<ICattleLookupService, CattleLookupService>();
    }

    [ExcludeFromCodeCoverage]
    private static void ConfigureMongo(IServiceCollection services, IConfiguration configuration)
    {
        MongoExtensions.Register();
        MongoConventions.Register();

        services
            .AddOptions<MongoConfig>()
            .Bind(configuration.GetRequiredSection("Mongo"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IMongoDbClientFactory, MongoDbClientFactory>();
    }

    [ExcludeFromCodeCoverage]
    private static void ConfigureOpenApi(IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Cattle Home API",
                    Version = "v1",
                    Description = "BE4FE API surface for the cattle-home Node application, including cached CPH, cattle list, and cattle detail lookups.",
                };

                return Task.CompletedTask;
            });
        });
    }

    [ExcludeFromCodeCoverage]
    private static WebApplication SetupApplication(WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.UseHeaderPropagation();
        app.UseExceptionHandler();
        app.UseRouting();

        app.MapOpenApi("/openapi/{documentName}.json");
        app.MapScalarApiReference();
        app.MapGet("/openapi", () => Results.Redirect("/openapi/v1.json"))
            .ExcludeFromDescription();

        app.UseHealthEndpoints();
        app.UseCattleEndpoints();
        app.UseCphEndpoints();
        app.UseUserEndpoints();

        return app;
    }
}
