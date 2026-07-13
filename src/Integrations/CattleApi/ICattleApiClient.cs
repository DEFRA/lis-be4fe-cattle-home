// <copyright file="ICattleApiClient.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.CattleHome.Integrations.CattleApi;

public interface ICattleApiClient
{
    Task<IReadOnlyCollection<CattleSummary>> GetCattleForCphAsync(string cph, CancellationToken cancellationToken = default);

    Task<CattleDetails> GetCattleDetailsAsync(string cattleId, CancellationToken cancellationToken = default);
}
