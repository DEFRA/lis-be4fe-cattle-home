// <copyright file="CattleApiCreateUserRequest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi.Dto;

using System.Text.Json.Serialization;

/// <summary>
/// The body of the cattle API's <c>POST v1/users</c>, which uses the OIDC claim names rather than the camelCase the
/// rest of the cattle API emits.
/// </summary>
public sealed record CattleApiCreateUserRequest(
    [property: JsonPropertyName("sub")] string? Sub,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("given_name")] string? GivenName,
    [property: JsonPropertyName("family_name")] string? FamilyName);
