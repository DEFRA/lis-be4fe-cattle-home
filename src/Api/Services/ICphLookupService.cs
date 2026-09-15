// <copyright file="ICphLookupService.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Services;

using Defra.Lis.Be4Fe.CattleApi;
using Defra.Lis.Be4Fe.Models.Lookups.Models;

public interface ICphLookupService
{
    Task<CachedLookupResponse<List<CattleSummary>>> GetCattleForCphAsync(string cph, CattleSearchQuery? query = null, CancellationToken cancellationToken = default);
}
