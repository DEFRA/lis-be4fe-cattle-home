// <copyright file="CattleHoldingRestClient.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi;

using System.Net;
using System.Text.Json;
using Defra.Lis.Be4Fe.CattleApi.Dto;
using Defra.Lis.Be4Fe.Models.Lookups.Models;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Rest;
using Microsoft.Extensions.Logging;

/// <summary>
/// Passes holding details and cattle-on-holding queries through to the cattle API using the
/// strategies SDK REST strategy. The API key travels as <c>x-api-key</c>; the correlation header is
/// added by header propagation on the SDK's HTTP client.
/// </summary>
public sealed partial class CattleHoldingRestClient(
    IRestStrategyFactory<CattleHoldingRestClient> strategyFactory,
    CattleApiOptions options,
    ILogger<CattleHoldingRestClient> logger)
    : ICattleHoldingClient
{
    public const string ApiKeyHeaderName = "x-api-key";

    private const string ApiDescription = "Cattle API";

    // The cattle API emits camelCase; the SDK's default is snake_case.
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<HoldingDetails> GetHoldingDetailsAsync(string cph, CancellationToken cancellationToken = default)
    {
        var segments = CphSegments.Parse(cph);

        try
        {
            var details = await BuildStrategy("Get holding details", cancellationToken)
                .WithResourceUrl($"holdings/{segments.ToRoute()}")
                .ExecuteAndTransform<CattleApiHolding, HoldingDetails>(holding => ToHoldingDetails(segments, holding));

            LogRetrievedHoldingDetails(segments.ToString());

            return details;
        }
        catch (RestResponseException ex) when (IsNotFound(ex))
        {
            LogHoldingNotFound(segments.ToString());
            throw new HoldingNotFoundException(segments.ToString());
        }
        catch (RestResponseException ex) when (IsBadRequest(ex))
        {
            throw new ArgumentException($"The cattle API rejected CPH '{segments}'.", nameof(cph), ex);
        }
    }

    public async Task<IReadOnlyCollection<CattleSummary>> SearchCattleAsync(string cph, CattleSearchQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var segments = CphSegments.Parse(cph);

        try
        {
            var strategy = BuildStrategy("Search cattle on holding", cancellationToken)
                .WithResourceUrl($"holdings/{segments.ToRoute()}/cattle")
                .WithQueryParameter(() => !string.IsNullOrWhiteSpace(query.Eartag), "earTag", query.Eartag?.Trim() ?? string.Empty)
                .WithQueryParameter(() => !string.IsNullOrWhiteSpace(query.Breed), "breed", query.Breed?.Trim() ?? string.Empty)
                .WithQueryParameter(() => !string.IsNullOrWhiteSpace(query.Sex), "sex", query.Sex?.Trim() ?? string.Empty);

            var cattle = await strategy.ExecuteAndTransform<List<CattleApiCattle>, IReadOnlyCollection<CattleSummary>>(
                list => list.Select(ToCattleSummary).ToList());

            LogRetrievedCattleForHolding(cattle.Count, segments.ToString(), !query.IsEmpty);

            return cattle;
        }
        catch (RestResponseException ex) when (IsNotFound(ex))
        {
            LogHoldingNotFound(segments.ToString());
            throw new HoldingNotFoundException(segments.ToString());
        }
        catch (RestResponseException ex) when (IsBadRequest(ex))
        {
            throw new ArgumentException($"The cattle API rejected CPH '{segments}'.", nameof(cph), ex);
        }
    }

    internal static HoldingDetails ToHoldingDetails(CphSegments requested, CattleApiHolding holding)
    {
        ArgumentNullException.ThrowIfNull(holding);

        return new HoldingDetails
        {
            Cph = string.IsNullOrWhiteSpace(holding.Cph) ? requested.ToString() : holding.Cph,
            Name = holding.Name,
            HoldingType = holding.HoldingType,
            RegisteredKeeper = holding.KeeperName,
            Address = holding.Address ?? [],
            HerdMarks = holding.HerdMarks ?? [],
            AllowedSpecies = holding.AllowedSpecies ?? [],
        };
    }

    internal static CattleSummary ToCattleSummary(CattleApiCattle cattle)
    {
        ArgumentNullException.ThrowIfNull(cattle);

        var earTag = cattle.EarTag ?? string.Empty;

        return new CattleSummary
        {
            CattleId = earTag,
            Eartag = earTag,
            Breed = cattle.Breed ?? cattle.BreedName ?? string.Empty,
            BreedCode = cattle.BreedCode,
            BreedName = cattle.BreedName,
            DateOfBirth = cattle.DateBirth,
            DateOnCph = cattle.DateOnCph,
            Sex = cattle.Sex ?? string.Empty,
            Status = cattle.Status ?? string.Empty,
        };
    }

    private static bool IsNotFound(RestResponseException exception) =>
        exception.InnerException is HttpRequestException { StatusCode: HttpStatusCode.NotFound };

    private static bool IsBadRequest(RestResponseException exception) =>
        exception.InnerException is HttpRequestException { StatusCode: HttpStatusCode.BadRequest };

    private IRestStrategy<CattleHoldingRestClient> BuildStrategy(string action, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            throw new InvalidOperationException($"{CattleApiOptions.SectionName}:BaseUrl must be configured to call the cattle API.");
        }

        var strategy = strategyFactory
            .BuildRestStrategy()
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithApiDescription(ApiDescription)
            .WithActionDescription(action)
            .WithBaseUrl(options.BaseUrl)
            .WithJsonSerializerOptions(SerializerOptions)
            .WithGet();

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            strategy.WithHeader(ApiKeyHeaderName, options.ApiKey);
        }

        return strategy;
    }
}
