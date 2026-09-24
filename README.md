# cattle-home

`cattle-home` is the BE4FE minimal API module for the Node `apps/cattle-home` flow.

The current foundation is intentionally small:

- minimal ASP.NET API surface
- MongoDB for interim and ephemeral state
- short-lived lookup caching in MongoDB
- fake KRDS and cattle providers behind interfaces that can be replaced later
- JSON-backed cattle data behind `ICattleApiClient`, ready to be replaced by the downstream HTTP client

## Structure

- `src/CattleHome` contains the application code
- `tests/CattleHome.Tests` contains smoke and endpoint tests
- `compose.yml` starts MongoDB and this API for local development

## Running locally

Start MongoDB only:

```bash
docker compose up -d mongodb
```

Run the API from the repo root:

```bash
dotnet run --project src/CattleHome/CattleHome.csproj --launch-profile CattleHome
```

The development profile wires:

- `Mongo__DatabaseUri=mongodb://127.0.0.1:27017/`
- `Mongo__DatabaseName=lis-be4fe-cattle-home`
- `CattleApi__BaseUrl=http://localhost:5019` (the `lis-api-cattle` `dotnet run` port)
- `CattleApi__ApiKey=` (sent as `x-api-key` when set; the cattle API does not enforce it locally)

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

User CPH responses are still read from `src/Api/Fixtures/CattleApi/cattle.json` until the cattle
API provides them. The path can be overridden with `CattleApi__FixturePath`; relative paths are
resolved from the published application directory.

## Local container stack

Bring up the API and MongoDB together:

```bash
docker compose up --build
```

## Endpoints

- `GET /` returns module metadata and whether Mongo and cattle API have been configured
- `GET /health` returns the liveness check
- `GET /openapi/v1.json` returns the OpenAPI v1 specification
- `GET /swagger/index.html` opens the interactive Swagger UI
- `GET /api/users/{userId}/cphs` returns CPHs for a user, using Mongo cache first
- `GET /api/cphs/{county}/{parish}/{holding}` returns holding details from the cattle API
- `GET /api/cphs/{county}/{parish}/{holding}/cattle?eartag=&breed=&sex=` returns live cattle on a CPH from the cattle API, optionally filtered
- `GET /api/cattle/{cattleId}` returns cattle details from the cattle API, using Mongo cache first; 404 when the animal is unknown

## Testing

Run the solution tests:

```bash
dotnet test be4fe-cattle-home.slnx
```
