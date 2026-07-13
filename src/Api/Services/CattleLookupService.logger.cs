namespace Defra.Lis.Be4Fe.Api.Services;

public partial class CattleLookupService
{
    [LoggerMessage(LogLevel.Debug, "Cache hit for cattle details. CattleId: {CattleId}")]
    partial void LogCacheHitForCattleDetailsCattleidCattleid(string cattleId);

    [LoggerMessage(LogLevel.Debug, "Cache miss for cattle details. Retrieving from API. CattleId: {CattleId}")]
    partial void LogCacheMissForCattleDetailsRetrievingFromApiCattleidCattleid(string cattleId);

    [LoggerMessage(LogLevel.Information, "Retrieved cattle details. CattleId: {CattleId}")]
    partial void LogRetrievedCattleDetailsCattleidCattleid(string cattleId);
}