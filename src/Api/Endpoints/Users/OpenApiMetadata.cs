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

    public static class CreateUser
    {
        public const string Name = "CreateUser";

        public const string Summary = "Creates or refreshes a user account.";

        public const string Description = "Ensures a user account exists in the keeper data service (through the cattle API) for the identity provider claims sub, email, given_name and family_name, and returns the user's details and associated CPHs (no caching). Always 200, whether the account was created or refreshed. 400 when a claim is missing or invalid, 409 when the email belongs to a different account, 503 when the keeper data service is temporarily unavailable.";
    }
}
