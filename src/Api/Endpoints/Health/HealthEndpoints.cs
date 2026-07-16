// <copyright file="HealthEndpoints.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Endpoints.Health;

using Defra.Lis.Be4Fe.Api.MetaData;
using Defra.Lis.Be4Fe.Models.Health;

public static class HealthEndpoints
{
    public static void UseHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(RouteNames.Health, CalculateHealthRoute)
            .WithName(OpenApiMetadata.Get.Name)
            .WithTags(OpenApiMetadata.Tag)
            .WithSummary(OpenApiMetadata.Get.Summary)
            .WithDescription(OpenApiMetadata.Get.Description)
            .WithMetadata(new IgnoreCorrelationIdCheck())
            .WithMetadata(new IgnoreApiKeyCheck());
    }

    private static Task<IResult> CalculateHealthRoute()
    {
        return Task.FromResult(Results.Ok(new HealthStatus() { Status = "ok" }));
    }
}
