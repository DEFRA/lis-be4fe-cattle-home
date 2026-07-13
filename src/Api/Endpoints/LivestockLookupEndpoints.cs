namespace Defra.Lis.Be4Fe.Api.Endpoints;

using Defra.Lis.Be4Fe.Api.Services;
using Defra.Lis.CattleHome.Integrations.CattleApi;
using Defra.Lis.CattleHome.Lookups.Models;

public static class LivestockLookupEndpoints
{
    public static IEndpointRouteBuilder MapLivestockLookupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Livestock lookups");

        group.MapGet("/users/{userId}/cphs", GetCphsForUser)
            .WithName("GetCphsForUser")
            .WithSummary("Gets the CPH holdings available to a user.")
            .WithDescription("Returns the holdings for a user from the Mongo cache when available, otherwise from the scaffolded CPH provider.")
            .Produces<CachedLookupResponse<List<UserCph>>>(StatusCodes.Status200OK);

        group.MapGet("/cphs/{cph}/cattle", GetCattleForCph)
            .WithName("GetCattleForCph")
            .WithSummary("Gets all cattle for a CPH.")
            .WithDescription("Returns the cattle list for a CPH from the Mongo cache when available, otherwise from the scaffolded cattle provider.")
            .Produces<CachedLookupResponse<List<CattleSummary>>>(StatusCodes.Status200OK);

        group.MapGet("/cattle/{cattleId}", GetCattleDetails)
            .WithName("GetCattleDetails")
            .WithSummary("Gets cattle details for a single animal.")
            .WithDescription("Returns cattle details from the Mongo cache when available, otherwise from the scaffolded cattle provider.")
            .Produces<CachedLookupResponse<CattleDetails>>(StatusCodes.Status200OK);

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

    private static async Task<IResult> GetCattleForCph(
        string cph,
        ICphLookupService cphLookupService,
        CancellationToken cancellationToken)
    {
        var response = await cphLookupService.GetCattleForCphAsync(cph, cancellationToken);
        return TypedResults.Ok(response);
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
