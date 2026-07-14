// <copyright file="FakeUserCphProvider.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Lookups.Providers;

using Defra.Lis.Be4Fe.Models.Lookups.Models;

public sealed class FakeUserCphProvider : IUserCphProvider
{
    public Task<IReadOnlyCollection<UserCph>> GetCphsForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var seed = DemoDataSeed.From(userId);
        var holdings = new List<UserCph>
        {
            new()
            {
                Cph = $"12/{100 + (seed % 900):000}/{1000 + (seed % 9000):0000}",
                Name = $"{userId} Home Farm",
            },
            new()
            {
                Cph = $"34/{100 + ((seed / 3) % 900):000}/{1000 + ((seed / 5) % 9000):0000}",
                Name = $"{userId} Hill Unit",
            },
        };

        return Task.FromResult<IReadOnlyCollection<UserCph>>(holdings);
    }
}
