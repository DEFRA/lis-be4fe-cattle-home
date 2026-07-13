using System.Diagnostics.CodeAnalysis;
using Defra.Lis.CattleHome.Integrations.CattleApi;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using Serilog;

namespace Defra.Lis.Be4Fe.Api;

using Defra.Lis.Be4Fe.Api.Config;
using Defra.Lis.Be4Fe.Api.Endpoints;
using Defra.Lis.Be4Fe.Api.Foundation.Caching;
using Defra.Lis.Be4Fe.Api.Lookups.Providers;
using Defra.Lis.Be4Fe.Api.Services;
using Defra.Lis.Be4Fe.Api.Utils;
using Defra.Lis.Be4Fe.Api.Utils.Http;
using Defra.Lis.Be4Fe.Api.Utils.Logging;
using Defra.Lis.Be4Fe.Api.Utils.Mongo;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var app = BuildApp(args);
        await app.RunAsync();
    }

    [ExcludeFromCodeCoverage]
    public static WebApplication BuildApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        ConfigureHost(builder);
        ConfigureServices(builder);

        var app = builder.Build();

        ConfigureMiddleware(app);
        ConfigureEndpoints(app);

        return app;
    }

    [ExcludeFromCodeCoverage]
    private static void ConfigureHost(WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog(CdpLogging.Configuration);
    }

    [ExcludeFromCodeCoverage]
    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

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

        services.AddOptions<CattleApiOptions>()
            .Bind(configuration.GetRequiredSection(CattleApiOptions.SectionName));

        var cattleApiBaseUrl = configuration.GetValue<string>($"{CattleApiOptions.SectionName}:BaseUrl");

        if (Uri.TryCreate(cattleApiBaseUrl, UriKind.Absolute, out var cattleApiBaseUri))
        {
            services.AddHttpClient(CattleApiHttpClient.Name, client =>
            {
                client.BaseAddress = cattleApiBaseUri;
            }).AddHeaderPropagation();
        }

        services.AddSingleton<IExternalDataCacheRepository, MongoExternalDataCacheRepository>();
        services.AddSingleton<ICachedDataService, CachedDataService>();
        services.AddSingleton<IUserCphProvider, FakeUserCphProvider>();
        services.AddSingleton<ICattleApiClient, FakeCattleApiClient>();
        services.AddSingleton<IUserLookupService, UserLookupService>();
        services.AddSingleton<ICphLookupService, CphLookupService>();
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
    private static void ConfigureMiddleware(WebApplication app)
    {
        app.UseSerilogRequestLogging();

        app.UseHeaderPropagation();
    }

    [ExcludeFromCodeCoverage]
    private static void ConfigureEndpoints(WebApplication app)
    {
        app.MapOpenApi("/openapi/{documentName}.json");
        app.MapGet("/openapi", () => Results.Redirect("/openapi/v1.json"))
            .ExcludeFromDescription();

        app.MapModuleEndpoints();
        app.MapLivestockLookupEndpoints();
        app.MapHealthChecks("/health", new HealthCheckOptions())
            .WithName("GetHealth")
            .WithTags("Health")
            .WithSummary("Gets the service health status.")
            .WithDescription("Returns the ASP.NET Core health status for this service.");
    }
}
