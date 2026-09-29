// <copyright file="IUserLookupService.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Services;

using Defra.Lis.Be4Fe.Models.Lookups.Models;

public interface IUserLookupService
{
    Task<CachedLookupResponse<List<UserCph>>> GetCphsForUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user's details and associated CPHs, passed straight through to the cattle API (no caching in this phase).
    /// </summary>
    Task<CachedLookupResponse<UserDetails>> GetUserDetailsAsync(string userId, CancellationToken cancellationToken = default);
}
