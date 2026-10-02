// <copyright file="HealthEndpoints.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Endpoints.Health;

using Defra.Lis.Be4Fe.Api.MetaData;
using Defra.Lis.Be4Fe.Models.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

public static class HealthEndpoints
{
    public static IEndpointConventionBuilder UseHealthEndpoints(this IEndpointRouteBuilder app)
    {
        return app.MapHealthChecks(
                RouteNames.Health,
                new HealthCheckOptions
                {
                    ResponseWriter = async (context, report) =>
                    {
                        var response = new HealthStatus
                        {
                            Status = report.Status.ToString(),
                        };
                        await context.Response.WriteAsJsonAsync(response, context.RequestAborted);
                    },
                })
            .WithName(OpenApiMetadata.Get.Name)
            .WithTags(OpenApiMetadata.Tag)
            .WithSummary(OpenApiMetadata.Get.Summary)
            .WithDescription(OpenApiMetadata.Get.Description)
            .WithMetadata(new IgnoreCorrelationIdCheck());
    }
}
