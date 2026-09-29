// <copyright file="ICattleHoldingClient.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi;

using Defra.Lis.Be4Fe.Models.Lookups.Models;

/// <summary>
/// Holding, animal and user lookups passed through to the cattle API.
/// </summary>
public interface ICattleHoldingClient
{
    /// <exception cref="ArgumentException">The CPH is not three segments.</exception>
    /// <exception cref="HoldingNotFoundException">The cattle API does not know the holding.</exception>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    Task<HoldingDetails> GetHoldingDetailsAsync(string cph, CancellationToken cancellationToken = default);

    /// <exception cref="ArgumentException">The CPH is not three segments.</exception>
    /// <exception cref="HoldingNotFoundException">The cattle API does not know the holding.</exception>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    Task<IReadOnlyCollection<CattleSummary>> SearchCattleAsync(string cph, CattleSearchQuery query, CancellationToken cancellationToken = default);

    /// <exception cref="ArgumentException">The ear tag is missing or blank.</exception>
    /// <exception cref="CattleNotFoundException">The cattle API does not know the animal.</exception>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    Task<CattleDetails> GetCattleDetailsAsync(string earTag, CancellationToken cancellationToken = default);

    /// <exception cref="ArgumentException">The user id is missing or blank, or the cattle API rejected it.</exception>
    /// <exception cref="UserNotFoundException">The cattle API does not know the user.</exception>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    Task<UserDetails> GetUserDetailsAsync(string userId, CancellationToken cancellationToken = default);
}
