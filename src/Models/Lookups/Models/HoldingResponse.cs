// <copyright file="HoldingResponse.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Lookups.Models;

public class HoldingResponse
{
    public required string Cph { get; init; }

    public string? Name { get; init; }

    public string? HoldingType { get; init; }

    public IReadOnlyCollection<string> Address { get; init; } = [];

    public string? KeeperName { get; init; }
}
