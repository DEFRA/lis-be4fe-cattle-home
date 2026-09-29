// <copyright file="UserNotFoundException.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi;

public sealed class UserNotFoundException(string userId)
    : Exception($"User '{userId}' was not found.")
{
    public string UserId { get; } = userId;
}
