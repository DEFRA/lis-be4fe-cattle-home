// <copyright file="ModuleEndpointsTest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.Foundation.Endpoints;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Defra.Lis.Be4Fe.Api;
using Defra.Lis.Be4Fe.Models.Health;
using Microsoft.AspNetCore.Mvc.Testing;

public class ModuleEndpointsTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public ModuleEndpointsTest(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealthShouldReturnHealthy()
    {
        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var health = await response.Content.ReadFromJsonAsync<HealthStatus>(TestContext.Current.CancellationToken);
        health.ShouldNotBeNull();
        health.Status.ShouldBe("Healthy");
    }

    [Fact]
    public async Task GetOpenApiDocumentShouldReturnDocumentedSpecification()
    {
        var response = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken);

        document.RootElement.GetProperty("info").GetProperty("title").GetString().ShouldBe("Cattle Home API");
        document.RootElement.GetProperty("paths").TryGetProperty("/api/users/{userId}/cphs", out _).ShouldBeTrue();

        var userDetails = document.RootElement.GetProperty("paths").GetProperty("/api/users/{userId}").GetProperty("get");
        userDetails.GetProperty("operationId").GetString().ShouldBe("GetUserDetails");
        userDetails.GetProperty("summary").GetString().ShouldBe("Gets a user's details and associated CPHs.");
        userDetails.GetProperty("responses").TryGetProperty("404", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task GetScalarUiShouldReturnInteractiveDocumentation()
    {
        var response = await client.GetAsync("/scalar/v1", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.ShouldContain("Scalar API Reference");
    }
}
