namespace Defra.Lis.Be4Fe.Api.Lookups.Providers;

using Defra.Lis.CattleHome.Lookups.Models;

public interface IUserCphProvider
{
    Task<IReadOnlyCollection<UserCph>> GetCphsForUserAsync(string userId, CancellationToken cancellationToken = default);
}
