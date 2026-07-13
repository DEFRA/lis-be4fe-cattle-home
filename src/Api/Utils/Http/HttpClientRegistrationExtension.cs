namespace Defra.Lis.Be4Fe.Api.Utils.Http;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public static class HttpClientRegistrationExtension
{
    public static IHttpClientBuilder AddHttpClientWithTracing<TClient, TImplementation>(
        this IServiceCollection services)
        where TClient : class
        where TImplementation : class, TClient
    {
        services.AddTransient<ProxyHttpMessageHandler>();

        return services
            .AddHttpClient<TClient, TImplementation>()
            .AddHeaderPropagation();
    }

    public static IHttpClientBuilder AddHttpClientWithTracingAndProxy<TClient, TImplementation>(
        this IServiceCollection services)
        where TClient : class
        where TImplementation : class, TClient
    {
        services.AddTransient<ProxyHttpMessageHandler>();

        return services
            .AddHttpClient<TClient, TImplementation>()
            .AddHeaderPropagation()
            .ConfigurePrimaryHttpMessageHandler<ProxyHttpMessageHandler>();
    }
}