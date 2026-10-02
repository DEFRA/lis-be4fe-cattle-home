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
/// Passes holding details, cattle-on-holding queries, single-animal details and user details through to the
/// cattle API using the strategies SDK REST strategy. The API key travels as <c>x-api-key</c>; the
/// correlation header is added by header propagation on the SDK's HTTP client.
/// </summary>
public sealed partial class CattleHoldingRestClient(
    IRestStrategyFactory<CattleHoldingRestClient> strategyFactory,
    CattleApiOptions options,
    ILogger<CattleHoldingRestClient> logger)
    : ICattleHoldingClient
{
    public const string ApiKeyHeaderName = "x-api-key";

    private const string ApiDescription = "Cattle API";

    // The cattle API serves every endpoint under a URL-segment version.
    private const string ApiVersion = "v1";

    // The cattle API emits camelCase; the SDK's default is snake_case.
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<HoldingDetails> GetHoldingDetailsAsync(string cph, CancellationToken cancellationToken = default)
    {
        var segments = CphSegments.Parse(cph);

        try
        {
            var details = await BuildStrategy("Get holding details", cancellationToken)
                .WithResourceUrl($"{ApiVersion}/holdings/{segments.ToRoute()}")
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
                .WithResourceUrl($"{ApiVersion}/holdings/{segments.ToRoute()}/cattle")
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

    public async Task<CattleDetails> GetCattleDetailsAsync(string earTag, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(earTag);

        var trimmedEarTag = earTag.Trim();

        try
        {
            var details = await BuildStrategy("Get cattle details", cancellationToken)
                .WithResourceUrl($"{ApiVersion}/cattle/{Uri.EscapeDataString(trimmedEarTag)}")
                .ExecuteAndTransform<CattleApiCattleDetails, CattleDetails>(ToCattleDetails);

            LogRetrievedCattleDetails(trimmedEarTag);

            return details;
        }
        catch (RestResponseException ex) when (IsNotFound(ex))
        {
            LogCattleNotFound(trimmedEarTag);
            throw new CattleNotFoundException(trimmedEarTag);
        }
    }

    public async Task<UserDetails> GetUserDetailsAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        // The user id is the identity provider subject: opaque and case-sensitive, so it is trimmed but never re-cased.
        var trimmedUserId = userId.Trim();

        try
        {
            var details = await BuildStrategy("Get user details", cancellationToken)
                .WithResourceUrl($"{ApiVersion}/users/{Uri.EscapeDataString(trimmedUserId)}")
                .ExecuteAndTransform<CattleApiUserDetails, UserDetails>(user => ToUserDetails(trimmedUserId, user));

            LogRetrievedUserDetails(details.Cphs.Count, trimmedUserId);

            return details;
        }
        catch (RestResponseException ex) when (IsNotFound(ex))
        {
            LogUserNotFound(trimmedUserId);
            throw new UserNotFoundException(trimmedUserId);
        }
        catch (RestResponseException ex) when (IsBadRequest(ex))
        {
            throw new ArgumentException($"The cattle API rejected user '{trimmedUserId}'.", nameof(userId), ex);
        }
    }

    internal static UserDetails ToUserDetails(string requestedUserId, CattleApiUserDetails user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserDetails
        {
            Subject = string.IsNullOrWhiteSpace(user.Subject) ? requestedUserId : user.Subject,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName,
            Cphs = (user.Cphs ?? [])
                .Where(cph => !string.IsNullOrWhiteSpace(cph.Cph))
                .Select(cph => new UserHolding
                {
                    Cph = cph.Cph!,
                    HoldingId = cph.HoldingId,
                    HoldingName = cph.HoldingName,
                    Role = cph.Role,
                })
                .ToList(),
        };
    }

    internal static CattleDetails ToCattleDetails(CattleApiCattleDetails cattle)
    {
        ArgumentNullException.ThrowIfNull(cattle);

        var earTag = cattle.EarTag ?? string.Empty;

        return new CattleDetails
        {
            CattleId = earTag,
            Eartag = earTag,

            // The cattle API reads details from CADS, which does not return the holding.
            Cph = null,
            Breed = cattle.BreedCode,
            BreedName = cattle.BreedName ?? cattle.Breed,
            Species = cattle.Species,
            Sex = cattle.Sex,
            DateOfBirth = cattle.DateBirth,
            DateRegistered = cattle.DateRegistered,
            DateOnCph = cattle.DateOnCph,
            State = cattle.State,
            RestrictionStatus = cattle.RestrictionStatus,
            DamType = cattle.DamType,
            GeneticDamTag = cattle.GeneticDamEarTag,
            SurrogateTag = cattle.SurrogateDamEarTag,
            SireTag = cattle.SireEarTag,
            SireName = cattle.SireName,
        };
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

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException($"{CattleApiOptions.SectionName}:ApiKey must be configured to call the cattle API.");
        }

        return strategyFactory
            .BuildRestStrategy()
            .WithLogger(logger)
            .WithCancellationToken(cancellationToken)
            .WithApiDescription(ApiDescription)
            .WithActionDescription(action)
            .WithBaseUrl(options.BaseUrl)
            .WithJsonSerializerOptions(SerializerOptions)
            .WithGet()
            .WithHeader(ApiKeyHeaderName, options.ApiKey);
    }
}
