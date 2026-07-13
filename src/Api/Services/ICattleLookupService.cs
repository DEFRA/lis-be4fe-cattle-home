namespace Defra.Lis.Be4Fe.Api.Services;

using Defra.Lis.CattleHome.Integrations.CattleApi;
using Defra.Lis.CattleHome.Lookups.Models;

public interface ICattleLookupService
{
    Task<CachedLookupResponse<CattleDetails>> GetCattleDetailsAsync(string cattleId, CancellationToken cancellationToken = default);
}
