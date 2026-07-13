namespace Defra.Lis.Be4Fe.Api.Services;

using Defra.Lis.CattleHome.Integrations.CattleApi;
using Defra.Lis.CattleHome.Lookups.Models;

public interface ICphLookupService
{
    Task<CachedLookupResponse<List<CattleSummary>>> GetCattleForCphAsync(string cph, CancellationToken cancellationToken = default);
}
