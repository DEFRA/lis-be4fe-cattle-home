// <copyright file="MongoExternalDataCacheRepositoryTest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.Foundation.Caching;

using System.Reflection;
using Defra.Lis.Be4Fe.Api.Foundation.Caching;
using Defra.Lis.Be4Fe.Api.Utils.Mongo;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;

public sealed class MongoExternalDataCacheRepositoryTest
{
    [Fact]
    public async Task TtlIndexShouldBeCreatedOnALaterCallWhenTheFirstAttemptFails()
    {
        var mongo = new FakeMongo(failuresBeforeSuccess: 1);
        var repository = CreateRepository(mongo);

        await repository.GetAsync<string>("user", "key", TestContext.Current.CancellationToken);
        mongo.IndexCreated.ShouldBeFalse();

        await repository.GetAsync<string>("user", "key", TestContext.Current.CancellationToken);

        mongo.IndexCreationAttempts.ShouldBe(2);
        mongo.IndexCreated.ShouldBeTrue();
        mongo.LastIndexName.ShouldBe("ttl_expiresAtUtc");
    }

    [Fact]
    public async Task TtlIndexShouldNotBeCreatedAgainOnceItExists()
    {
        var mongo = new FakeMongo(failuresBeforeSuccess: 0);
        var repository = CreateRepository(mongo);

        await repository.GetAsync<string>("user", "key", TestContext.Current.CancellationToken);
        await repository.SetAsync("user", "key", "value", DateTimeOffset.UtcNow.AddMinutes(5), TestContext.Current.CancellationToken);
        await repository.GetAsync<string>("user", "key", TestContext.Current.CancellationToken);

        mongo.IndexCreationAttempts.ShouldBe(1);
    }

    [Fact]
    public async Task CacheCallsShouldStillWorkAndKeepRetryingWhileTheIndexCannotBeCreated()
    {
        var mongo = new FakeMongo(failuresBeforeSuccess: int.MaxValue);
        var repository = CreateRepository(mongo);

        var result = await repository.GetAsync<string>("user", "key", TestContext.Current.CancellationToken);
        await repository.SetAsync("user", "key", "value", DateTimeOffset.UtcNow.AddMinutes(5), TestContext.Current.CancellationToken);

        result.ShouldBeNull();
        mongo.ReplaceCount.ShouldBe(1);
        mongo.IndexCreationAttempts.ShouldBe(2);
        mongo.IndexCreated.ShouldBeFalse();
    }

    private static MongoExternalDataCacheRepository CreateRepository(FakeMongo mongo)
    {
        return new MongoExternalDataCacheRepository(mongo, NullLogger<MongoExternalDataCacheRepository>.Instance);
    }

    /// <summary>
    /// Routes every call on a proxied interface to a handler.
    /// </summary>
    public class CallRouter : DispatchProxy
    {
        private Func<MethodInfo, object?[], object?> handler = (method, _) => throw new NotSupportedException(method.Name);

        public static T Create<T>(Func<MethodInfo, object?[], object?> handler)
            where T : class
        {
            var proxy = Create<T, CallRouter>();
            ((CallRouter)(object)proxy).handler = handler;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            return handler(targetMethod, args ?? []);
        }
    }

    /// <summary>
    /// Just enough of the Mongo driver for the repository: index creation that fails a set number of times, an empty
    /// find and an acknowledged replace. The driver interfaces are too large to implement by hand, so the collection,
    /// index manager and cursor are <see cref="DispatchProxy"/> instances that route the calls back here.
    /// </summary>
    private sealed class FakeMongo(int failuresBeforeSuccess) : IMongoDbClientFactory
    {
        public int IndexCreationAttempts { get; private set; }

        public bool IndexCreated { get; private set; }

        public string? LastIndexName { get; private set; }

        public int ReplaceCount { get; private set; }

        public IMongoClient GetClient()
        {
            throw new NotSupportedException();
        }

        public IMongoCollection<T> GetCollection<T>(string collection)
        {
            return (IMongoCollection<T>)(object)CallRouter.Create<IMongoCollection<ExternalDataCacheDocument>>(HandleCollectionCall);
        }

        private static object? HandleCursorCall(MethodInfo method, object?[] args)
        {
            return method.Name switch
            {
                "MoveNextAsync" => Task.FromResult(false),
                "MoveNext" => false,
                "get_Current" => Array.Empty<ExternalDataCacheDocument>(),
                "Dispose" => null,
                _ => throw new NotSupportedException($"IAsyncCursor.{method.Name} is not faked."),
            };
        }

        private object? HandleCollectionCall(MethodInfo method, object?[] args)
        {
            return method.Name switch
            {
                "get_Indexes" => CallRouter.Create<IMongoIndexManager<ExternalDataCacheDocument>>(HandleIndexCall),
                "FindAsync" => Task.FromResult(CallRouter.Create<IAsyncCursor<ExternalDataCacheDocument>>(HandleCursorCall)),
                "ReplaceOneAsync" => Replace(),
                _ => throw new NotSupportedException($"IMongoCollection.{method.Name} is not faked."),
            };
        }

        private Task<ReplaceOneResult> Replace()
        {
            ReplaceCount++;
            return Task.FromResult<ReplaceOneResult>(new ReplaceOneResult.Acknowledged(0, 0, null));
        }

        private object? HandleIndexCall(MethodInfo method, object?[] args)
        {
            if (method.Name != "CreateOneAsync" || args[0] is not CreateIndexModel<ExternalDataCacheDocument> model)
            {
                throw new NotSupportedException($"IMongoIndexManager.{method.Name} is not faked.");
            }

            IndexCreationAttempts++;

            if (IndexCreationAttempts <= failuresBeforeSuccess)
            {
                return Task.FromException<string>(new TimeoutException("Mongo is unavailable."));
            }

            IndexCreated = true;
            LastIndexName = model.Options.Name;
            return Task.FromResult(model.Options.Name);
        }
    }
}
