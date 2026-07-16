// <copyright file="OpenApiMetadata.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Endpoints.Cattle;

using Defra.Lis.Be4Fe.Api;

public static class OpenApiMetadata
{
    public const string Tag = nameof(RouteNames.Cattle);

    public static class GetCattleDetails
    {
        public const string Name = "GetCattleDetails";
        public const string Summary = "Gets cattle details for a single animal.";
        public const string Description = "Returns cattle details from the Mongo cache when available, otherwise from the scaffolded cattle provider.";
    }
}
