// <copyright file="IHoldingLookupService.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Services;

using Defra.Lis.Be4Fe.Models.Lookups.Models;

public interface IHoldingLookupService
{
    Task<CachedLookupResponse<HoldingDetailedResponse>> GetHoldingDetailsAsync(string cph, CancellationToken cancellationToken = default);

    Task<PaginatedResponse<HoldingResponse>> SearchHoldingsAsync(HoldingSearchQuery query, CancellationToken cancellationToken = default);
}
