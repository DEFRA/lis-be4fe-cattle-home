// <copyright file="HoldingDetailedResponse.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Models.Lookups.Models;

public sealed class HoldingDetailedResponse : HoldingResponse
{
    public IReadOnlyCollection<HoldingMarkResponse> HerdMarks { get; init; } = [];

    public IReadOnlyCollection<string> AllowedSpecies { get; init; } = [];

    public IReadOnlyCollection<HoldingAssociationResponse> Associations { get; init; } = [];
}
