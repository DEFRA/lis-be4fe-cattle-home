// <copyright file="ServiceToServiceEndpointsTest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.Authentication;

using System.Net;
using Defra.Lis.Be4Fe.Api;
using Defra.Lis.Be4Fe.Api.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

public sealed class ServiceToServiceEndpointsTest : IClassFixture<ServiceToServiceEndpointsTest.KeyedFactory>
{
    private readonly KeyedFactory factory;

    public ServiceToServiceEndpointsTest(KeyedFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public void EveryApiEndpointRequiresTheServiceToServicePolicy()
    {
        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/api", StringComparison.Ordinal) == true)
            .ToList();

        endpoints.ShouldNotBeEmpty();
        endpoints.ShouldAllBe(endpoint => endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Any(data => data.Policy == AuthPolicies.ServiceToService));
    }

    [Theory]
    [InlineData("/api/users/alice/cphs")]
    [InlineData("/api/users/0b6f2f0e-3c1a-4e8e-9d4b-2f6a1c9e7d51")]
    [InlineData("/api/cphs/22/001/0001")]
    [InlineData("/api/cphs/22/001/0001/cattle")]
    [InlineData("/api/cattle/UK200000000001")]
    public async Task ApiRequestWithoutTheKeyShouldBeRejected(string path)
    {
        var response = await factory.CreateClient().GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task ApiRequestWithTheWrongKeyShouldBeRejected()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/cphs/22/001/0001");
        request.Headers.Add("x-api-key", "not-the-key");

        var response = await factory.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/openapi/v1.json")]
    public async Task HealthAndOpenApiShouldStayAnonymous(string path)
    {
        var response = await factory.CreateClient().GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    public sealed class KeyedFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting($"{ApiKeyAuthenticationOptions.SectionName}:Keys:0", TestAuthentication.ApiKey);
        }
    }
}
