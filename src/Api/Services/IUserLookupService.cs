// <copyright file="IUserLookupService.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Services;

using Defra.Lis.Be4Fe.Models.Lookups.Models;

public interface IUserLookupService
{
    /// <summary>
    /// Gets the CPHs a user is associated with in KRDS, passed straight through to the cattle API (no caching).
    /// </summary>
    Task<CachedLookupResponse<List<UserHolding>>> GetCphsForUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user's details and associated CPHs, passed straight through to the cattle API (no caching in this phase).
    /// </summary>
    Task<CachedLookupResponse<UserDetails>> GetUserDetailsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates (or refreshes) a user account through the cattle API from the identity provider claims and returns the
    /// user's details, wrapped like the other lookups (no caching).
    /// </summary>
    /// <param name="request">The identity provider claims (sub, email, given_name, family_name).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user's details, with the cattle API as the source.</returns>
    Task<CachedLookupResponse<UserDetails>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
}
