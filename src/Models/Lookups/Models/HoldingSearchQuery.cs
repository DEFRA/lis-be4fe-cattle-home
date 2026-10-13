// <copyright file="HoldingSearchQuery.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Lookups.Models;

/// <summary>
/// Search, paging and sorting query parameters for the holdings lookup; passed through to the cattle API.
/// </summary>
public sealed record HoldingSearchQuery(
    string? Search = null,
    int? Page = null,
    int? PageSize = null,
    string? Sort = null,
    string? Order = null)
{
    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Search) &&
        !Page.HasValue &&
        !PageSize.HasValue &&
        string.IsNullOrWhiteSpace(Sort) &&
        string.IsNullOrWhiteSpace(Order);
}
