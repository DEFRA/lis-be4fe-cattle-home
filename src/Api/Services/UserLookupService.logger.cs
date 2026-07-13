namespace Defra.Lis.Be4Fe.Api.Services;

public partial class UserLookupService
{
    [LoggerMessage(LogLevel.Debug, "Cache hit for user CPHs. UserId: {UserId}")]
    partial void LogCacheHitForUserCphsUseridUserid(string userId);

    [LoggerMessage(LogLevel.Debug, "Cache miss for user CPHs. Retrieving from provider. UserId: {UserId}")]
    partial void LogCacheMissForUserCphsRetrievingFromProviderUseridUserid(string userId);

    [LoggerMessage(LogLevel.Information, "Retrieved {Count} CPHs for user. UserId: {UserId}")]
    partial void LogRetrievedCountCphsForUserUseridUserid(int count, string userId);
}