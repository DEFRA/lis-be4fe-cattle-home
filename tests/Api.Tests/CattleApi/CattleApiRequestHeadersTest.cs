// <copyright file="CattleApiRequestHeadersTest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.CattleApi;

using System.Net;
using Defra.Lis.Be4Fe.Api;
using Defra.Lis.Be4Fe.CattleApi;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Rest.Client;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Rest.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Proves the production wiring: a request to the BE4FE reaches the cattle API through the SDK client
/// carrying the propagated x-cdp-request-id and the configured x-api-key.
/// </summary>
public sealed class CattleApiRequestHeadersTest
{
    [Fact]
    public async Task OutboundCattleApiRequestShouldCarryTheCorrelationIdAndApiKey()
    {
        var handler = new StubHttpMessageHandler().RespondWith(HttpStatusCode.OK, "[]");
        await using var factory = new HeaderCaptureFactory(handler);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-cdp-request-id", "corr-12345");

        var response = await client.GetAsync("/api/cphs/22/001/0001/cattle?sex=female", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var outbound = handler.Requests.ShouldHaveSingleItem();
        outbound.RequestUri!.ToString().ShouldBe("http://cattle-api.test/holdings/22/001/0001/cattle?sex=female");
        outbound.Headers.GetValues("x-cdp-request-id").ShouldBe(["corr-12345"]);
        outbound.Headers.GetValues(CattleHoldingRestClient.ApiKeyHeaderName).ShouldBe(["configured-key"]);
    }

    private sealed class HeaderCaptureFactory(StubHttpMessageHandler handler) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("CattleApi:BaseUrl", "http://cattle-api.test/");
            builder.UseSetting("CattleApi:ApiKey", "configured-key");

            builder.ConfigureTestServices(services =>
            {
                services.AddHttpClient<IRestHttpClient, RestHttpClient>()
                    .ConfigurePrimaryHttpMessageHandler(() => handler);
            });
        }
    }
}
