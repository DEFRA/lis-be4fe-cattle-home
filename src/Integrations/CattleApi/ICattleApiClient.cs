// <copyright file="ICattleApiClient.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi;

using Defra.Lis.Be4Fe.Models.Lookups.Models;

public interface ICattleApiClient
{
    Task<IReadOnlyCollection<UserCph>> GetCphsForUserAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CattleSummary>> GetCattleForCphAsync(string cph, CancellationToken cancellationToken = default);

    Task<CattleDetails> GetCattleDetailsAsync(string cattleId, CancellationToken cancellationToken = default);
}
