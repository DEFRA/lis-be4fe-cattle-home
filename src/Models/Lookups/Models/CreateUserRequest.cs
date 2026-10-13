// <copyright file="CreateUserRequest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Lookups.Models;

/// <summary>
/// The identity provider claims used to create (or refresh) a user account. Bound from the snake_case body
/// (<c>sub</c>, <c>email</c>, <c>given_name</c>, <c>family_name</c>), which matches the OIDC claim names.
/// </summary>
public sealed class CreateUserRequest
{
    public string? Sub { get; init; }

    public string? Email { get; init; }

    public string? GivenName { get; init; }

    public string? FamilyName { get; init; }
}
