// <copyright file="UserLookupService.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Services;

using Defra.Lis.Be4Fe.CattleApi;
using Defra.Lis.Be4Fe.Models.Lookups.Models;

public sealed partial class UserLookupService(
    ICattleHoldingClient cattleHoldingClient,
    ILogger<UserLookupService> logger)
    : IUserLookupService
{
    public async Task<CachedLookupResponse<List<UserHolding>>> GetCphsForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        // Not cached, like the user details: the associations are refreshed in KRDS every time the user signs in.
        var details = await cattleHoldingClient.GetUserDetailsAsync(userId, cancellationToken);
        var cphs = details.Cphs.ToList();
        LogRetrievedCphsForUser(cphs.Count);

        return new CachedLookupResponse<List<UserHolding>>
        {
            Source = LookupSources.CattleApi,
            CachedUntilUtc = null,
            Data = cphs,
        };
    }

    public async Task<CachedLookupResponse<UserDetails>> GetUserDetailsAsync(string userId, CancellationToken cancellationToken = default)
    {
        // The subject is opaque and case-sensitive, so it is passed through unchanged.
        var details = await cattleHoldingClient.GetUserDetailsAsync(userId, cancellationToken);

        return new CachedLookupResponse<UserDetails>
        {
            Source = LookupSources.CattleApi,
            CachedUntilUtc = null,
            Data = details,
        };
    }

    public async Task<CachedLookupResponse<UserDetails>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        // Not cached: the account is refreshed in KRDS on every call.
        var details = await cattleHoldingClient.CreateUserAsync(request, cancellationToken);

        return new CachedLookupResponse<UserDetails>
        {
            Source = LookupSources.CattleApi,
            CachedUntilUtc = null,
            Data = details,
        };
    }
}
