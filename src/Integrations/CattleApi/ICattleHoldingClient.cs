// <copyright file="ICattleHoldingClient.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi;

using Defra.Lis.Be4Fe.Models.Lookups.Models;

/// <summary>
/// Holding-level lookups passed through to the cattle API.
/// </summary>
public interface ICattleHoldingClient
{
    /// <exception cref="ArgumentException">The CPH is not three segments.</exception>
    /// <exception cref="HoldingNotFoundException">The cattle API does not know the holding.</exception>
    Task<HoldingDetails> GetHoldingDetailsAsync(string cph, CancellationToken cancellationToken = default);

    /// <exception cref="ArgumentException">The CPH is not three segments.</exception>
    /// <exception cref="HoldingNotFoundException">The cattle API does not know the holding.</exception>
    Task<IReadOnlyCollection<CattleSummary>> SearchCattleAsync(string cph, CattleSearchQuery query, CancellationToken cancellationToken = default);
}
