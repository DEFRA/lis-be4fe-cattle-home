// <copyright file="UserHolding.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Lookups.Models;

/// <summary>
/// A CPH a user is associated with, and the role they hold on it.
/// </summary>
public sealed class UserHolding
{
    public required string Cph { get; init; }

    public string? HoldingId { get; init; }

    public string? HoldingName { get; init; }

    public string? Role { get; init; }
}
