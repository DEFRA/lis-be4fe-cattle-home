namespace Defra.Lis.Be4Fe.Api.Foundation.Caching;

public sealed class CacheLookupResult<T>
{
    public required T Value { get; init; }

    public required DateTimeOffset ExpiresAtUtc { get; init; }
}
