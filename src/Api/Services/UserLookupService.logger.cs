// <copyright file="UserLookupService.logger.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Services;

public partial class UserLookupService
{
    // The Defra ID subject is personal data, so it is never logged.
    [LoggerMessage(LogLevel.Information, "Retrieved {Count} CPHs for the user from the cattle API.")]
    partial void LogRetrievedCphsForUser(int count);
}
