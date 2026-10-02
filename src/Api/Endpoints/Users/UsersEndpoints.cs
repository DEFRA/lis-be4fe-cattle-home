// <copyright file="UsersEndpoints.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Endpoints.Users;

using Defra.Lis.Be4Fe.Api.Authentication;
using Defra.Lis.Be4Fe.Api.Services;
using Defra.Lis.Be4Fe.Models.Lookups.Models;

public static class UsersEndpoints
{
    public static IEndpointRouteBuilder UseUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api")
            .RequireServiceToServiceAuthorization()
            .WithTags(OpenApiMetadata.Tag);

        group.MapGet(RouteNames.Users + "/{userId}/cphs", GetCphsForUser)
            .WithName(OpenApiMetadata.GetCphsForUser.Name)
            .WithSummary(OpenApiMetadata.GetCphsForUser.Summary)
            .WithDescription(OpenApiMetadata.GetCphsForUser.Description)
            .Produces<CachedLookupResponse<List<UserCph>>>(StatusCodes.Status200OK);

        group.MapGet(RouteNames.Users + "/{userId}", GetUserDetails)
            .WithName(OpenApiMetadata.GetUserDetails.Name)
            .WithSummary(OpenApiMetadata.GetUserDetails.Summary)
            .WithDescription(OpenApiMetadata.GetUserDetails.Description)
            .Produces<CachedLookupResponse<UserDetails>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetCphsForUser(
        string userId,
        IUserLookupService userLookupService,
        CancellationToken cancellationToken)
    {
        var response = await userLookupService.GetCphsForUserAsync(userId, cancellationToken);
        return TypedResults.Ok(response);
    }

    private static async Task<IResult> GetUserDetails(
        string userId,
        IUserLookupService userLookupService,
        CancellationToken cancellationToken)
    {
        var response = await userLookupService.GetUserDetailsAsync(userId, cancellationToken);
        return TypedResults.Ok(response);
    }
}
