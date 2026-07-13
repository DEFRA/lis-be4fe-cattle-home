namespace Defra.Lis.Be4Fe.Api.Endpoints;

using System.Reflection;
using Defra.Lis.Be4Fe.Api.Config;
using Defra.Lis.CattleHome.Foundation.Models;
using Defra.Lis.CattleHome.Integrations.CattleApi;

public static class ModuleEndpoints
{
    public static IEndpointRouteBuilder MapModuleEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", GetModuleInfo)
            .WithName("GetModuleInfo")
            .WithTags("Module")
            .WithSummary("Gets service metadata for the cattle-home BE4FE module.")
            .WithDescription("Returns the running service metadata, version information, and dependency configuration status.")
            .Produces<ModuleInfoResponse>(StatusCodes.Status200OK);

        return app;
    }

    private static IResult GetModuleInfo(
        IHostEnvironment hostEnvironment,
        IConfiguration configuration)
    {
        var informationalVersion = typeof(Program).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? typeof(Program).Assembly.GetName().Version?.ToString()
            ?? "unknown";

        var response = new ModuleInfoResponse
        {
            Service = "cattle-home",
            Environment = hostEnvironment.EnvironmentName,
            Version = informationalVersion,
            Dependencies = new DependencyConfiguration
            {
                Mongo = new DependencyStatus
                {
                    Configured = IsConfigured(configuration.GetSection(MongoConfig.SectionName)["DatabaseUri"]),
                },
                CattleApi = new DependencyStatus
                {
                    Configured = IsConfigured(configuration.GetSection(CattleApiOptions.SectionName)["BaseUrl"]),
                },
            },
        };

        return TypedResults.Ok(response);
    }

    private static bool IsConfigured(string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }
}
