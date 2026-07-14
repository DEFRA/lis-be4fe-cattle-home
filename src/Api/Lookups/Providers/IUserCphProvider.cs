// <copyright file="IUserCphProvider.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Lookups.Providers;

using Defra.Lis.Be4Fe.Models.Lookups.Models;

public interface IUserCphProvider
{
    Task<IReadOnlyCollection<UserCph>> GetCphsForUserAsync(string userId, CancellationToken cancellationToken = default);
}
