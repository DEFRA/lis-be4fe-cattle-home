// <copyright file="CattleHoldingRestClient.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.CattleApi;

using System.Globalization;
using System.Net;
using System.Text.Json;
using Defra.Lis.Be4Fe.CattleApi.Dto;
using Defra.Lis.Be4Fe.Models.Lookups.Models;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Rest;
using Microsoft.Extensions.Logging;

/// <summary>
/// Passes holding details, cattle-on-holding queries, single-animal details, user details and user creation through
/// to the cattle API using the strategies SDK REST strategy. The API key travels as <c>x-api-key</c>; the
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

    public async Task<HoldingDetailedResponse> GetHoldingDetailsAsync(string cph, CancellationToken cancellationToken = default)
    {
        var segments = CphSegments.Parse(cph);

        try
        {
            var details = await BuildStrategy("Get holding details", cancellationToken)
                .WithResourceUrl($"{ApiVersion}/holdings/{segments.ToRoute()}")
                .ExecuteAndTransform<CattleApiHoldingDetailed, HoldingDetailedResponse>(holding => ToHoldingDetails(segments, holding));

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

    public async Task<PaginatedResponse<HoldingResponse>> SearchHoldingsAsync(HoldingSearchQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            var result = await BuildStrategy("Search holdings", cancellationToken)
                .WithResourceUrl($"{ApiVersion}/holdings")
                .WithQueryParameter(() => !string.IsNullOrWhiteSpace(query.Search), "Search", query.Search?.Trim() ?? string.Empty)
                .WithQueryParameter(() => query.Page.HasValue, "Page", query.Page?.ToString(CultureInfo.InvariantCulture) ?? string.Empty)
                .WithQueryParameter(() => query.PageSize.HasValue, "PageSize", query.PageSize?.ToString(CultureInfo.InvariantCulture) ?? string.Empty)
                .WithQueryParameter(() => !string.IsNullOrWhiteSpace(query.Sort), "Sort", query.Sort?.Trim() ?? string.Empty)
                .WithQueryParameter(() => !string.IsNullOrWhiteSpace(query.Order), "Order", query.Order?.Trim() ?? string.Empty)
                .ExecuteAndTransform<CattleApiPaginatedResponse<CattleApiHolding>, PaginatedResponse<HoldingResponse>>(ToPaginatedHoldings);

            LogRetrievedHoldings(result.Items.Count, result.TotalCount, result.Page, result.TotalPages, !query.IsEmpty);

            return result;
        }
        catch (RestResponseException ex) when (IsBadRequest(ex))
        {
            throw new ArgumentException("The cattle API rejected the holdings search query.", nameof(query), ex);
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

    public async Task<UserDetails> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // The claims are validated by the cattle API; the subject is the only one logged, as the rest are personal data.
        var subject = request.Sub?.Trim() ?? string.Empty;
        var payload = new CattleApiCreateUserRequest(request.Sub, request.Email, request.GivenName, request.FamilyName);

        try
        {
            var details = await BuildStrategy("Create user", cancellationToken)
                .WithPost()
                .WithResourceUrl($"{ApiVersion}/users")
                .WithPayload(payload)
                .ExecuteAndTransform<CattleApiUserDetails, UserDetails>(user => ToUserDetails(subject, user));

            LogCreatedUser(details.Cphs.Count, subject);

            return details;
        }
        catch (RestResponseException ex) when (IsBadRequest(ex))
        {
            throw new ArgumentException("The cattle API rejected the user account claims.", nameof(request), ex);
        }
        catch (RestResponseException ex) when (HasStatus(ex, HttpStatusCode.Conflict))
        {
            LogUserConflict(subject);
            throw new UserConflictException(subject);
        }
        catch (RestResponseException ex) when (HasStatus(ex, HttpStatusCode.ServiceUnavailable))
        {
            LogCattleApiUnavailable("Create user");
            throw new ServiceUnavailableException("The user account service is temporarily unavailable, try again later.", ex);
        }
    }

    private static UserDetails ToUserDetails(string requestedUserId, CattleApiUserDetails user)
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

    private static CattleDetails ToCattleDetails(CattleApiCattleDetails cattle)
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
            CurrentLocation = ToCattleLocation(cattle.CurrentLocation),
            RegisteredLocation = ToCattleLocation(cattle.RegisteredLocation),
            DeathLocation = ToCattleLocation(cattle.DeathLocation),
            State = cattle.State,
            RestrictionStatus = cattle.RestrictionStatus,
            DamType = cattle.DamType,
            GeneticDamTag = cattle.GeneticDamEarTag,
            SurrogateTag = cattle.SurrogateDamEarTag,
            SireTag = cattle.SireEarTag,
            SireName = cattle.SireName,
        };
    }

    private static CattleLocation? ToCattleLocation(CattleApiCattleLocation? location)
    {
        if (location is null)
        {
            return null;
        }

        return new CattleLocation
        {
            Identifier = location.Identifier,
            SubIdentifier = location.SubIdentifier,
            FullIdentifier = location.FullIdentifier,
            Name = location.Name,
            PremisesType = location.PremisesType,
        };
    }

    private static HoldingDetailedResponse ToHoldingDetails(CphSegments requested, CattleApiHoldingDetailed holdingDetailed)
    {
        ArgumentNullException.ThrowIfNull(holdingDetailed);

        return new HoldingDetailedResponse
        {
            Cph = string.IsNullOrWhiteSpace(holdingDetailed.Cph) ? requested.ToString() : holdingDetailed.Cph,
            Name = holdingDetailed.Name,
            HoldingType = holdingDetailed.HoldingType,
            KeeperName = holdingDetailed.KeeperName,
            StartDate = holdingDetailed.StartDate,
            EndDate = holdingDetailed.EndDate,
            Active = holdingDetailed.Active,
            Address = holdingDetailed.Address ?? [],
            HerdMarks = holdingDetailed.HerdMarks?
                .Select(m => new HoldingMarkResponse
                {
                    Mark = m.Mark,
                    StartDate = m.StartDate,
                    EndDate = m.EndDate,
                    Species = m.Species ?? [],
                })
                .ToList() ?? [],
            AllowedSpecies = holdingDetailed.AllowedSpecies ?? [],
            Associations = holdingDetailed.Associations?
                .Select(a => new HoldingAssociationResponse
                {
                    CustomerNumber = a.CustomerNumber,
                    Title = a.Title,
                    FirstName = a.FirstName,
                    LastName = a.LastName,
                    Name = a.Name,
                    PartyType = a.PartyType,
                    Email = a.Email,
                    Mobile = a.Mobile,
                    Telephone = a.Telephone,
                    Address = a.Address ?? [],
                    Roles = a.Roles?
                        .Select(r => new HoldingRoleResponse
                        {
                            Code = r.Code,
                            Species = r.Species ?? [],
                        })
                        .ToList() ?? [],
                })
                .ToList() ?? [],
        };
    }

    private static HoldingResponse ToHoldingSummary(CattleApiHolding holding)
    {
        ArgumentNullException.ThrowIfNull(holding);

        return new HoldingResponse
        {
            Cph = holding.Cph ?? string.Empty,
            Name = holding.Name,
            HoldingType = holding.HoldingType,
            Address = holding.Address ?? [],
            KeeperName = holding.KeeperName,
            StartDate = holding.StartDate,
            EndDate = holding.EndDate,
            Active = holding.Active,
        };
    }

    private static PaginatedResponse<HoldingResponse> ToPaginatedHoldings(CattleApiPaginatedResponse<CattleApiHolding> response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new PaginatedResponse<HoldingResponse>
        {
            Count = response.Count,
            TotalCount = response.TotalCount,
            Items = (response.Items ?? []).Select(ToHoldingSummary).ToList(),
            Page = response.Page,
            PageSize = response.PageSize,
            TotalPages = response.TotalPages,
            HasNextPage = response.HasNextPage,
            HasPreviousPage = response.HasPreviousPage,
        };
    }

    private static CattleSummary ToCattleSummary(CattleApiCattle cattle)
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

    private static bool IsNotFound(RestResponseException exception) => HasStatus(exception, HttpStatusCode.NotFound);

    private static bool IsBadRequest(RestResponseException exception) => HasStatus(exception, HttpStatusCode.BadRequest);

    private static bool HasStatus(RestResponseException exception, HttpStatusCode statusCode) =>
        exception.InnerException is HttpRequestException { StatusCode: { } actual } && actual == statusCode;

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
