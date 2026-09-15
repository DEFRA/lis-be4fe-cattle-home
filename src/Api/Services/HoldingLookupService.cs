// <copyright file="HoldingLookupService.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Services;

using Defra.Lis.Be4Fe.CattleApi;
using Defra.Lis.Be4Fe.Models.Lookups.Models;

/// <summary>
/// Holding details, passed straight through to the cattle API (no caching in this phase).
/// </summary>
public sealed partial class HoldingLookupService(
    ICattleHoldingClient cattleHoldingClient,
    ILogger<HoldingLookupService> logger)
    : IHoldingLookupService
{
    public async Task<CachedLookupResponse<HoldingDetails>> GetHoldingDetailsAsync(string cph, CancellationToken cancellationToken = default)
    {
        var normalisedCph = cph.Trim().ToUpperInvariant();
        var details = await cattleHoldingClient.GetHoldingDetailsAsync(normalisedCph, cancellationToken);
        LogRetrievedHoldingDetailsCphCph(normalisedCph);

        return new CachedLookupResponse<HoldingDetails>
        {
            Source = LookupSources.CattleApi,
            CachedUntilUtc = null,
            Data = details,
        };
    }
}
