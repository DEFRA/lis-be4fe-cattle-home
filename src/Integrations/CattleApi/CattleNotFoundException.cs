// <copyright file="CattleNotFoundException.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi;

public sealed class CattleNotFoundException(string earTag)
    : Exception($"Cattle '{earTag}' was not found.")
{
    public string EarTag { get; } = earTag;
}
