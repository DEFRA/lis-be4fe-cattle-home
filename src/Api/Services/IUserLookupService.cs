namespace Defra.Lis.Be4Fe.Api.Services;

using Defra.Lis.CattleHome.Lookups.Models;

public interface IUserLookupService
{
    Task<CachedLookupResponse<List<UserCph>>> GetCphsForUserAsync(string userId, CancellationToken cancellationToken = default);
}
