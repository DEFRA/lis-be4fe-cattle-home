// <copyright file="HoldingAssociationResponse.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Lookups.Models;

public sealed class HoldingAssociationResponse
{
    public string? CustomerNumber { get; init; }

    public string? Title { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? Name { get; init; }

    public string? PartyType { get; init; }

    public string? Email { get; init; }

    public string? Mobile { get; init; }

    public string? Telephone { get; init; }

    public IReadOnlyCollection<string> Address { get; init; } = [];

    public IReadOnlyCollection<HoldingRoleResponse> Roles { get; init; } = [];
}
