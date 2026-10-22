# lis-be4fe-cattle-home

Part of the Livestock Information Service (LIS), which lets livestock keepers, owners, agents and
delegates record and manage their animals.

This is the backend-for-frontend (BE4FE) API for the cattle home microsite. It gives the
`lis-apps-cattle-home` spoke one API for the holdings a user can access and the cattle on them, and
passes each request on to `lis-api-cattle`. It is a .NET 10 minimal API that keeps short-lived lookup
results in MongoDB.

Deployed on the Core Delivery Platform (CDP). See the
[CDP portal service page](https://portal.cdp-int.defra.cloud/services/lis-be4fe-cattle-home) for
environments, deployments, logs and metrics.

## Architecture

```text
hub (front-office or back-office) → lis-apps-cattle-home → this API → lis-api-cattle → KRDS and CADS
```

- Callers: `lis-apps-cattle-home` only. This is an internal CDP service with no sign-in of its own.
- Authentication: every `/api` endpoint needs an `x-api-key` header that matches one of the keys in
  `ApiKeyAuthentication__Keys__N`, or it returns `401`. The spoke sends its key as `CATTLE_HOME_API_KEY`.
  `/health`, the OpenAPI document and Scalar are anonymous. The endpoints use the `ServiceToService`
  authorisation policy, so another scheme can be added to that policy later. Every `/api` request also
  needs an `x-cdp-request-id` header (see [Correlation ID](#correlation-id)).
- Downstream calls: holding, cattle and user data comes from `lis-api-cattle`, through a REST strategy from the
  `Defra.Livestock.Sdk.Api.Strategies` package. Each call carries the `x-api-key` header from
  `CattleApi__ApiKey` and the caller's `x-cdp-request-id`. The call goes straight over the internal CDP
  network, without the outbound proxy. This API does not send or check an AWS STS service token.
- Data store: MongoDB (database `lis-be4fe-cattle-home`) holds cached cattle details, kept for
  `Cache__DurationMinutes` minutes. Holding, cattle list and user lookups are not cached.

Cattle details come from CADS through `lis-api-cattle`, which does not return the holding. So `cph` is
null and `sire_name` is not supplied until the upstream contract carries them.

See [HUB-ARCHITECTURE.md](https://github.com/DEFRA/lis-dev/blob/main/HUB-ARCHITECTURE.md) for the hub and spoke design.

### Structure

- `src/Api` is the application: endpoints, authentication, middleware, services and caching
- `src/Integrations/CattleApi` is the client for `lis-api-cattle`
- `src/Models` holds the shared response models
- `tests/Api.Tests` holds the unit and endpoint tests
- `tests/Endpoints` holds `.http` requests for every `/api` endpoint (see [Running tests](#running-tests))
- `compose.yml` starts MongoDB and this API on their own
- `build.cake` and `base.cake` are the Cake build scripts

### Endpoints

Paths under `/api` need the `x-api-key` and `x-cdp-request-id` headers.

| Method and path | Purpose |
|---|---|
| `GET /health` | Health check |
| `GET /openapi/v1.json` | OpenAPI v1 document (`/openapi` redirects to it) |
| `GET /scalar/v1` | Interactive Scalar API reference |
| `GET /api/users/{userId}/cphs` | CPHs the user is associated with in KRDS, with holding name and role. `404` if the user is unknown, `503` if the cattle API or KRDS is unavailable |
| `GET /api/users/{userId}` | User details and associated CPHs. `404` if the user is unknown |
| `POST /api/users` | Creates or refreshes a user in KRDS from the claims `sub`, `email`, `given_name` and `family_name`. `400` for a missing or invalid claim, `409` if the email belongs to another account, `503` if KRDS is unavailable |
| `GET /api/cphs/{county}/{parish}/{holding}` | Holding details. `404` if the holding is unknown |
| `GET /api/cphs?search=&page=&pageSize=&sort=&order=` | Paged search of holdings in KRDS |
| `GET /api/cphs/{county}/{parish}/{holding}/cattle?eartag=&breed=&sex=` | Live cattle on a CPH, optionally filtered |
| `GET /api/cattle/{cattleId}` | Cattle details, from the MongoDB cache first. `404` if the animal is unknown |

Errors use problem details.

### Correlation ID

Every `/api` endpoint needs a single `x-cdp-request-id` header, as set out in the
[Correlation ID standard](https://eaflood.atlassian.net/wiki/spaces/LDD/pages/6438093586/Correlation+ID+standard).
Without it the request is rejected with `400` and `"code": "missing_header"` before authentication runs.
The value is returned on the response and as `correlationId` in error problem details, written as `CorrelationId`
on every log line, and sent on to `lis-api-cattle`. When calling the API by hand, send your own, for example
`-H 'x-cdp-request-id: local-test-1'`.

## Prerequisites

- The .NET SDK named in `global.json` (10.0.0 or later; check with `dotnet --version`)
- Docker with Docker Compose
- A local clone of [lis-dev](https://github.com/DEFRA/lis-dev) (recommended, see [Running with lis-dev](#running-with-lis-dev-recommended))

## Setup

### Development

```bash
dotnet restore be4fe-cattle-home.slnx
dotnet build be4fe-cattle-home.slnx
```

There is no `.env` file. Configuration comes from `appsettings.json`, `appsettings.<Environment>.json` and
environment variables, in that order (see [Configuration](#configuration)). The `Development` launch profile in
`src/Api/Properties/launchSettings.json` sets the values needed to run on the host.

### Test

There are no tools to install beyond the .NET SDK. Tests use xUnit and need no Docker or database.

## Running with lis-dev (recommended)

This API only works properly alongside `lis-apps-cattle-home`, `lis-api-cattle`, MongoDB and the fake identity
providers. [lis-dev](https://github.com/DEFRA/lis-dev) runs all of these locally with Docker Compose and serves
them behind local HTTPS hostnames, so it is the recommended way to run and work on this repo. Follow its README
for the one-time setup.

Start the stack:

```bash
cd lis-dev
docker compose up -d
```

In compose this API is the `lis-be4fe-cattle-home` service, published on port 8087 of the host (8085 inside the
container). It uses MongoDB from the stack and `lis-api-cattle` on `host.docker.internal:8091`.

lis-dev's `sideload` only supports Node.js services, so it cannot swap this one. To work on this API
with the rest of the stack still in Docker, stop its container and run it on the host on the same port:

```bash
cd lis-dev
docker compose stop lis-be4fe-cattle-home

cd be4fe/cattle-home
ASPNETCORE_URLS=http://localhost:8087 \
Mongo__DatabaseUri=mongodb://127.0.0.1:27017/ \
CattleApi__BaseUrl=http://localhost:8091 \
CattleApi__ApiKey=local-dev-cattle-api-key \
ApiKeyAuthentication__Keys__0=local-dev-cattle-home-api-key \
dotnet run --project src/Api --no-launch-profile
```

These values match what the compose file sets, with the host names changed to `localhost`: MongoDB and
`lis-api-cattle` publish ports 27017 and 8091 on the host. `lis-apps-cattle-home` still reaches the API at
`host.docker.internal:8087` with the key `local-dev-cattle-home-api-key`. `--no-launch-profile` stops the
`Development` profile from replacing these values and moving the API to port 8085. Use `dotnet watch` in place of
`dotnet run` to restart on changes. When done, press Ctrl+C and run `docker compose start lis-be4fe-cattle-home`.

Then open <https://front-office.lis.defra/cattle> and sign in through the fake identity provider.

## Running standalone

You can also run the API on its own with the `Development` launch profile:

```bash
docker compose up -d mongodb
dotnet run --project src/Api --launch-profile Development   # http://localhost:8085
```

`/health` and `/scalar/v1` work without any other service, because the cattle API options are only checked when a
lookup is made. The `/api` endpoints need MongoDB on `127.0.0.1:27017` and `lis-api-cattle` on
`http://localhost:5019` (change `CattleApi__BaseUrl` if it runs elsewhere).

To run this API and MongoDB together in containers, without lis-dev:

```bash
docker compose up --build
```

This publishes the API on <http://localhost:3225> and expects `lis-api-cattle` on `host.docker.internal:5019`.

### Configuration

Settings are in `src/Api/appsettings.json`, with `appsettings.Development.json` for local use. Each setting can be
overridden by an environment variable, with `__` in place of `:`. Values marked secret must be set as secrets in
the CDP portal for every deployed environment.

| Variable | Description | Required | Default |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Selects `appsettings.<Environment>.json` | no | `Production` |
| `ASPNETCORE_URLS` | Address and port to bind. The container sets `http://+:8085` | no | .NET default |
| `Mongo__DatabaseUri` | MongoDB connection string. CDP replaces the placeholder with the platform's AWS-authenticated URI | yes | `mongodb://set-automatically-when-deployed/admin?authSource=$external&authMechanism=MONGODB-AWS` (`mongodb://127.0.0.1:27017` in Development) |
| `Mongo__DatabaseName` | MongoDB database name | yes | `lis-be4fe-cattle-home` |
| `CattleApi__BaseUrl` | Base URL of `lis-api-cattle`. CDP sets it per environment | for lookups | empty (`http://localhost:5019` in Development) |
| `CattleApi__ApiKey` | Secret. Key sent to `lis-api-cattle` as `x-api-key`. Must match one of that API's `ApiKeyAuthentication__Keys__N` | for lookups | empty |
| `ApiKeyAuthentication__Keys__0` | Secret. Key callers must send as `x-api-key`. Must equal `CATTLE_HOME_API_KEY` in `lis-apps-cattle-home` | yes | none |
| `ApiKeyAuthentication__Keys__1` | Secret. Optional second key, so a key can be rotated without downtime | no | none |
| `ApiKeyAuthentication__HeaderName` | Header that carries the key | no | `x-api-key` |
| `Cache__DurationMinutes` | Minutes cattle details stay in the MongoDB cache (1 to 1440) | no | `5` |
| `TraceHeader` | Header copied from the request to downstream calls | no | `x-cdp-request-id` |
| `AllowedHosts` | ASP.NET allowed hosts | no | `*` |
| `Serilog__MinimumLevel__Default` | Log level | no | `Information` |
| `HTTP_PROXY`, `HTTPS_PROXY`, `NO_PROXY` | Outbound proxy settings, read by the default .NET handler | no | none |
| `TRUSTSTORE_*` | CDP-supplied certificates, loaded into the trust store at start-up | no | none |

With no inbound key configured, the API starts and answers `/health` but rejects every `/api` request. With no
`CattleApi__BaseUrl` or `CattleApi__ApiKey`, it also starts, but lookups fail with a configuration error.
On CDP, logs go to the console in Elastic Common Schema format.

## Running tests

```bash
dotnet test be4fe-cattle-home.slnx      # unit and endpoint tests
dotnet format be4fe-cattle-home.slnx    # format the code; the build also runs StyleCop and Sonar analysers
```

`tests/Api.Tests` covers authentication, the correlation ID check, error handling, the cattle API client, caching
and every endpoint. The endpoint tests run in memory with the cattle API client and the MongoDB cache replaced, so
they need no Docker or database.

### Endpoint requests (`tests/Endpoints`)

`tests/Endpoints` holds JetBrains HTTP client requests, with asserts, for every `/api` endpoint, grouped by area
(`cphs`, `cattle`, `users`, `health`). `security/Security.http` checks that a missing or wrong `x-api-key` gives
`401` and that a missing `x-cdp-request-id` gives `400 missing_header`. Run them from the IDE or with `ijhttp`:

```bash
cd tests/Endpoints
ijhttp --env-file http-client.env.json --env local health/Health.http users/Users.http cphs/Cphs.http cattle/Cattle.http security/Security.http
```

- `local` expects this API on 8085, the port of the `Development` launch profile. The lis-dev stack runs it on 8087
  instead, so to test that, set `host` to `http://localhost:8087` in `http-client.env.json`. It also expects `lis-api-cattle` on 5019 and lis-fake-service on 3000, using the Oakfield fixtures.
- `dev-operational` runs the same requests through a local `lis-api-cattle` that calls KRDS and CADS in CDP dev. See
  "Running locally against CDP dev" in the lis-api-cattle README. Replace the placeholder `cph`, `cattleId` and
  `subject` with identifiers that exist in dev, and do not commit responses, because they contain real keeper data.

Cattle details are cached in MongoDB, so clear the cache before checking that a cattle details call reaches the upstream.

End-to-end journeys are covered by [lis-journey-tests](https://github.com/DEFRA/lis-journey-tests).

## Monitoring

- Health check: `GET /health` returns `{"status":"Healthy"}`. It shows the API is running and does not call MongoDB or `lis-api-cattle`.
- Logs: OpenSearch dashboard per environment, linked from the [CDP portal service page](https://portal.cdp-int.defra.cloud/services/lis-be4fe-cattle-home) under Logs
- Metrics: Grafana dashboard per environment, linked from the [CDP portal service page](https://portal.cdp-int.defra.cloud/services/lis-be4fe-cattle-home) under Metrics

## Pipelines

Build, versioning and publishing run in GitHub Actions (`.github/workflows`) using Cake scripts (`*.cake`):

- **Pull request to `main`**: format, build and test, Sonar analysis, version check
- **Push to a `feature/` or `hotfix/` branch**: prerelease build and tag
- **Merge to `main`**: release tag, then publish the container image to CDP

Versions come from git tags (SemVer). The `Dockerfile` runs the tests and publishes the API.

## Branching policy

The repo uses mainline development. Branch from `main` and name the branch:

- `feature/LREG-<ticket>-<short-kebab-description>`
- `hotfix/LREG-<ticket>-<short-kebab-description>`

`LREG` must be uppercase, because the GitHub ruleset rejects other branch names. Rebase onto `main`
rather than merging `main` in. Incomplete work goes behind a feature flag.

## Contributing to this project

- Follow the [Defra software development standards](https://github.com/DEFRA/software-development-standards).
- Before you push, run `dotnet format be4fe-cattle-home.slnx` and `dotnet test be4fe-cattle-home.slnx`. Fix StyleCop and Sonar warnings rather than suppress them.
- Open a pull request against `main` using the PR template. CI must pass and the Sonar quality gate must be green before merge.

## Licence

THIS INFORMATION IS LICENSED UNDER THE CONDITIONS OF THE OPEN GOVERNMENT LICENCE found at:

<http://www.nationalarchives.gov.uk/doc/open-government-licence/version/3>

The following attribution statement MUST be cited in your products and applications when using this information.

> Contains public sector information licensed under the Open Government licence v3

### About the licence

The Open Government Licence (OGL) was developed by the Controller of His Majesty's Stationery Office (HMSO) to enable information providers in the public sector to license the use and re-use of their information under a common open licence.

It is designed to encourage use and re-use of information freely and flexibly, with only a few conditions.
