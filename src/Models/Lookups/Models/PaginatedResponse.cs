// <copyright file="PaginatedResponse.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Lookups.Models;

public sealed class PaginatedResponse<T>
{
    public int Count { get; init; }

    public int TotalCount { get; init; }

    public IReadOnlyCollection<T> Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalPages { get; init; }

    public bool HasNextPage { get; init; }

    public bool HasPreviousPage { get; init; }
}
