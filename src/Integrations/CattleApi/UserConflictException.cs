// <copyright file="UserConflictException.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi;

/// <summary>
/// The cattle API refused to create a user account because its email belongs to a different account.
/// </summary>
public sealed class UserConflictException(string userId)
    : Exception("The email address is already associated with a different user account.")
{
    public string UserId { get; } = userId;
}
