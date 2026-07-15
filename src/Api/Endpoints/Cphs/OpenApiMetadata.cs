// <copyright file="OpenApiMetadata.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Endpoints.Cphs;

using Defra.Lis.Be4Fe.Api;

public static class OpenApiMetadata
{
    public const string Tag = nameof(RouteNames.CountyParishHoldings);

    public static class GetCattleForCph
    {
        public const string Name = "GetCattleForCph";
        public const string Summary = "Gets all cattle for a CPH.";
        public const string Description = "Returns the cattle list for a CPH from the Mongo cache when available, otherwise from the scaffolded cattle provider.";
    }
}
