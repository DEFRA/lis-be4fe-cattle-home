// <copyright file="CattleApiPaginatedResponse.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi.Dto;

/// <summary>
/// Paginated response from the cattle API (camelCase JSON).
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed record CattleApiPaginatedResponse<T>(
    int Count,
    int TotalCount,
    IReadOnlyList<T>? Items,
    int Page,
    int PageSize,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage);
