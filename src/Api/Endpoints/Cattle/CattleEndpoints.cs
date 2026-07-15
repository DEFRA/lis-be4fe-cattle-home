// <copyright file="CattleEndpoints.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Endpoints.Cattle;

using Defra.Lis.Be4Fe.Api.Services;
using Defra.Lis.Be4Fe.CattleApi;
using Defra.Lis.Be4Fe.Models.Lookups.Models;

public static class CattleEndpoints
{
    public static IEndpointRouteBuilder UseCattleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api")
            .WithTags(OpenApiMetadata.Tag);

        group.MapGet(RouteNames.Cattle + "/{cattleId}", GetCattleDetails)
            .WithName(OpenApiMetadata.GetCattleDetails.Name)
            .WithSummary(OpenApiMetadata.GetCattleDetails.Summary)
            .WithDescription(OpenApiMetadata.GetCattleDetails.Description)
            .Produces<CachedLookupResponse<CattleDetails>>(StatusCodes.Status200OK);

        return app;
    }

    private static async Task<IResult> GetCattleDetails(
        string cattleId,
        ICattleLookupService cattleLookupService,
        CancellationToken cancellationToken)
    {
        var response = await cattleLookupService.GetCattleDetailsAsync(cattleId, cancellationToken);
        return TypedResults.Ok(response);
    }
}
