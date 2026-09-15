// <copyright file="CphsEndpoints.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Endpoints.Cphs;

using Defra.Lis.Be4Fe.Api.Services;
using Defra.Lis.Be4Fe.CattleApi;
using Defra.Lis.Be4Fe.Models.Lookups.Models;

public static class CphsEndpoints
{
    public static IEndpointRouteBuilder UseCphEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api")
            .WithTags(OpenApiMetadata.Tag);

        group.MapGet(RouteNames.CountyParishHoldings + "/{county}/{parish}/{holding}", GetHoldingDetails)
            .WithName(OpenApiMetadata.GetHoldingDetails.Name)
            .WithSummary(OpenApiMetadata.GetHoldingDetails.Summary)
            .WithDescription(OpenApiMetadata.GetHoldingDetails.Description)
            .Produces<CachedLookupResponse<HoldingDetails>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet(RouteNames.CountyParishHoldings + "/{county}/{parish}/{holding}/cattle", GetCattleForCph)
            .WithName(OpenApiMetadata.GetCattleForCph.Name)
            .WithSummary(OpenApiMetadata.GetCattleForCph.Summary)
            .WithDescription(OpenApiMetadata.GetCattleForCph.Description)
            .Produces<CachedLookupResponse<List<CattleSummary>>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetHoldingDetails(
        string county,
        string parish,
        string holding,
        IHoldingLookupService holdingLookupService,
        CancellationToken cancellationToken)
    {
        var response = await holdingLookupService.GetHoldingDetailsAsync($"{county}/{parish}/{holding}", cancellationToken);
        return TypedResults.Ok(response);
    }

    private static async Task<IResult> GetCattleForCph(
        string county,
        string parish,
        string holding,
        [AsParameters] CattleSearchQuery query,
        ICphLookupService cphLookupService,
        CancellationToken cancellationToken)
    {
        var response = await cphLookupService.GetCattleForCphAsync($"{county}/{parish}/{holding}", query, cancellationToken);
        return TypedResults.Ok(response);
    }
}
