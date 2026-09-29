// <copyright file="OpenApiMetadata.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Endpoints.Users;

using Defra.Lis.Be4Fe.Api;

public static class OpenApiMetadata
{
    public const string Tag = nameof(RouteNames.Users);

    public static class GetCphsForUser
    {
        public const string Name = "GetCphsForUser";
        public const string Summary = "Gets the CPH holdings available to a user.";
        public const string Description = "Returns the holdings for a user from the Mongo cache when available, otherwise from the scaffolded CPH provider.";
    }

    public static class GetUserDetails
    {
        public const string Name = "GetUserDetails";
        public const string Summary = "Gets a user's details and associated CPHs.";
        public const string Description = "Returns the user's details and the CPHs they were associated with when they last signed in, from the cattle API (no caching). The user id is the identity provider (Defra ID) subject. 404 when the user is unknown.";
    }
}
