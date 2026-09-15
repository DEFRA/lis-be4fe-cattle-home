// <copyright file="OpenApiMetadata.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Endpoints.Cphs;

using Defra.Lis.Be4Fe.Api;

public static class OpenApiMetadata
{
    public const string Tag = nameof(RouteNames.CountyParishHoldings);

    public static class GetHoldingDetails
    {
        public const string Name = "GetHoldingDetails";
        public const string Summary = "Gets the holding details for a CPH.";
        public const string Description = "Returns the holding details (name, address, keeper, herd marks, allowed species) for a county/parish/holding number from the cattle API.";
    }

    public static class GetCattleForCph
    {
        public const string Name = "GetCattleForCph";
        public const string Summary = "Gets the live cattle on a CPH, optionally filtered.";
        public const string Description = "Returns the live cattle on a county/parish/holding number from the cattle API. Optional eartag, breed (code or name) and sex query parameters filter the result.";
    }
}
