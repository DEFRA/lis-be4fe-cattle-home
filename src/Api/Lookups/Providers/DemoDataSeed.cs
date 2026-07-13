// <copyright file="DemoDataSeed.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Lookups.Providers;

internal static class DemoDataSeed
{
    public static int From(string input)
    {
        var seed = 17;

        foreach (var character in input.ToUpperInvariant())
        {
            seed = (seed * 31) + character;
        }

        return Math.Abs(seed);
    }
}
