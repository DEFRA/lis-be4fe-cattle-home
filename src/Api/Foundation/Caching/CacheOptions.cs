// <copyright file="CacheOptions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Foundation.Caching;

using System.ComponentModel.DataAnnotations;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    [Range(1, 1440)]
    public int DurationMinutes { get; init; } = 5;
}
