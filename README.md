# cattle-home

`cattle-home` is the BE4FE minimal API module for the Node `apps/cattle-home` flow.

The current foundation is intentionally small:

- minimal ASP.NET API surface
- MongoDB for interim and ephemeral state
- short-lived lookup caching in MongoDB
- fake KRDS and cattle providers behind interfaces that can be replaced later

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
- `CattleApi__BaseUrl=http://localhost:5000`

Update `CattleApi__BaseUrl` to match the local `api/cattle` host if it differs.

## Local container stack

Bring up the API and MongoDB together:

```bash
docker compose up --build
```

## Endpoints

- `GET /` returns module metadata and whether Mongo and cattle API have been configured
- `GET /health` returns the liveness check
- `GET /openapi/v1.json` is available in development
- `GET /api/users/{userId}/cphs` returns CPHs for a user, using Mongo cache first
- `GET /api/cphs/{cph}/cattle` returns cattle for a CPH, using Mongo cache first
- `GET /api/cattle/{cattleId}` returns cattle details, using Mongo cache first

## Testing

Run the solution tests:

```bash
dotnet test be4fe-cattle-home.slnx
```
