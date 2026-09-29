// <copyright file="UserDetails.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Lookups.Models;

/// <summary>
/// A user's details and the CPHs they were associated with when they last signed in.
/// </summary>
public sealed class UserDetails
{
    public required string Subject { get; init; }

    public string? Email { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? DisplayName { get; init; }

    public IReadOnlyCollection<UserHolding> Cphs { get; init; } = [];
}
