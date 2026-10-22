# cattle-home

`cattle-home` is the BE4FE minimal API module for the Node `apps/cattle-home` flow.

The current foundation is intentionally small:

- minimal ASP.NET API surface
- MongoDB for interim and ephemeral state
- short-lived lookup caching in MongoDB
- holding, cattle and user lookups passed through to the cattle API (`lis-api-cattle`), which reads KRDS and CADS

## Structure

- `src/Api` contains the application code, with the cattle API client in `src/Integrations/CattleApi` and shared
  models in `src/Models`
- `tests/Api.Tests` and `tests/Integrations/CattleApi.Tests` contain the unit and endpoint tests
- `tests/Endpoints` contains `.http` requests for every `/api` endpoint (see Testing)
- `compose.yml` starts MongoDB and this API for local development

## Running locally

Start MongoDB only:

```bash
docker compose up -d mongodb
```

Run the API from the repo root:

```bash
dotnet run --project src/Api --launch-profile Development   # http://localhost:8085
```

The development profile wires:

- `Mongo__DatabaseUri=mongodb://127.0.0.1:27017/`
- `Mongo__DatabaseName=lis-be4fe-cattle-home`
- `CattleApi__BaseUrl=http://localhost:5019` (the `lis-api-cattle` `dotnet run` port)
- `CattleApi__ApiKey=local-dev-cattle-api-key` (sent to the cattle API as `x-api-key`; required, and must match the
  cattle API's `ApiKeyAuthentication__Keys__0`)
- `ApiKeyAuthentication__Keys__0=local-dev-cattle-home-api-key` (the key callers must send; see below)

Update `CattleApi__BaseUrl` to match the local cattle API host if it differs (the cattle API's
compose stack publishes it on 8085).

Holding details, the cattle-on-holding list and single-animal cattle details are passed through
to the cattle API via a `Defra.Livestock.Sdk.Api.Strategies` REST strategy
(`src/Integrations/CattleApi/CattleHoldingRestClient.cs`); the inbound `x-cdp-request-id` header is
propagated. The cattle API in turn reads LIS-FAKE. Holding and cattle-list responses are not cached
in this phase; cattle details are cached in Mongo. `CattleApi__BaseUrl` is only validated when a
lookup is made, so the service starts (and answers `/health`) without it.

Cattle details come from CADS through the cattle API, which does not return the holding, so `cph`
is null and `sire_name` is unsupplied until the upstream contract carries them.

The user's CPHs (`GET /api/users/{userId}/cphs`) are the KRDS associations returned by the cattle
API's `GET /v1/users/{subject}`, passed through without caching. No fixture data ships with the
service.

The cattle API is an internal CDP service, so it is called directly over the internal CDP network,
without the outbound proxy (the default .NET handler still honours `HTTP(S)_PROXY` and `NO_PROXY`).

## Authentication

Every `/api` endpoint requires an `x-api-key` header matching one of the configured keys, otherwise it returns
`401` problem details. `/health`, the OpenAPI document and Scalar stay anonymous. This is the interim mechanism
agreed in [LREG-560](https://eaflood.atlassian.net/browse/LREG-560) until AWS STS replaces it; the endpoints only
reference the `ServiceToService` authorisation policy, so STS is added as another scheme on that policy.

- `ApiKeyAuthentication__Keys__0`: accepted key, set as a CDP secret. It must equal `CATTLE_HOME_API_KEY` in
  `lis-apps-cattle-home`.
- `ApiKeyAuthentication__Keys__1`: optional second key, so the key can be rotated without downtime.
- `CattleApi__ApiKey`: the key this service sends to the cattle API. A lookup without it fails with a configuration
  error, and a 401 or 403 from the cattle API surfaces as a 500, never as the caller's own 401.

With no inbound key configured the service still starts and answers `/health`, but rejects every `/api` request.

## Correlation ID

Every `/api` endpoint also requires a single `x-cdp-request-id` header (the
[Correlation ID standard](https://eaflood.atlassian.net/wiki/spaces/LDD/pages/6438093586/Correlation+ID+standard));
without it the request is rejected with `400` and `"code": "missing_header"` before authentication runs. The
cattle-home UI always sends it (`@defra/lis-hubs-infra-core`); `/health`, the OpenAPI document and Scalar do not need
it. The value is echoed on the response, returned as `correlationId` in error problem details, written as
`CorrelationId` on every log line and forwarded to the cattle API. Send one yourself when calling the service by
hand, for example `-H 'x-cdp-request-id: local-test-1'`.

## Local container stack

Bring up the API and MongoDB together:

```bash
docker compose up --build
```

## Endpoints

- `GET /health` returns the liveness check
- `GET /openapi/v1.json` returns the OpenAPI v1 specification (`/openapi` redirects to it)
- `GET /scalar/v1` opens the interactive Scalar API reference
- `GET /api/users/{userId}/cphs` returns the CPHs a user is associated with in KRDS, with holding name and role, from the cattle API (no caching); 404 when the user is unknown, 503 when the cattle API reports that it or KRDS is temporarily unavailable
- `GET /api/users/{userId}` returns a user's details and associated CPHs from the cattle API (no caching); 404 when the user is unknown
- `POST /api/users` creates or refreshes a user account in KRDS through the cattle API from the claims `sub`, `email`, `given_name` and `family_name`, returning the user's details (no caching). Always 200; 400 for a missing or invalid claim, 409 when the email belongs to another account, 503 while KRDS is temporarily unavailable. See `tests/Endpoints/users/Users.http`
- `GET /api/cphs/{county}/{parish}/{holding}` returns holding details from the cattle API; 404 when the holding is unknown
- `GET /api/cphs?search=&page=&pageSize=&sort=&order=` searches holdings in KRDS through the cattle API, paged
- `GET /api/cphs/{county}/{parish}/{holding}/cattle?eartag=&breed=&sex=` returns live cattle on a CPH from the cattle API, optionally filtered
- `GET /api/cattle/{cattleId}` returns cattle details from the cattle API, using Mongo cache first; 404 when the animal is unknown

## Testing

Run the solution tests:

```bash
dotnet test be4fe-cattle-home.slnx
```

### Endpoint requests (`tests/Endpoints`)

`tests/Endpoints` holds JetBrains HTTP client requests, with asserts, for every `/api` endpoint, grouped by area
(`cphs`, `cattle`, `users`, `health`). `security/Security.http` checks that a missing or wrong `x-api-key` gives
`401` and that a missing `x-cdp-request-id` gives `400 missing_header`. Run them from the IDE or with `ijhttp`:

```bash
cd tests/Endpoints
ijhttp --env-file http-client.env.json --env local health/Health.http users/Users.http cphs/Cphs.http cattle/Cattle.http security/Security.http
```

- `local` expects this API on 8085, the cattle API on 5019 and lis-fake-service on 3000, using the Oakfield fixtures.
- `dev-operational` runs the same requests through a local cattle API that calls KRDS and CADS in CDP dev. See
  "Running locally against CDP dev" in the lis-api-cattle README. Replace the placeholder `cph`, `cattleId` and
  `subject` with identifiers that exist in dev, and don't commit responses, because they contain real keeper data.

Cattle details are cached in Mongo, so clear the cache before checking a cattle details call reaches the upstream.
