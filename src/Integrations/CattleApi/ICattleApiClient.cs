// <copyright file="ICattleApiClient.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi;

using Defra.Lis.Be4Fe.Models.Lookups.Models;

/// <summary>
/// Lookups still served from fixture data until the cattle API provides them.
/// Holding details and cattle on a holding come from <see cref="ICattleHoldingClient"/>.
/// </summary>
public interface ICattleApiClient
{
    Task<IReadOnlyCollection<UserCph>> GetCphsForUserAsync(string userId, CancellationToken cancellationToken = default);

    Task<CattleDetails> GetCattleDetailsAsync(string cattleId, CancellationToken cancellationToken = default);
}
