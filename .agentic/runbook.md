# Runbook

## Run locally
API (port 5106, Development seeds invented demo data):
```bash
dotnet build src/Huddle.Api/Huddle.Api.csproj
ASPNETCORE_URLS=http://localhost:5106 ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Huddle.Api --no-build
curl -H "X-Member-Id: m-thandi" http://localhost:5106/api/groups/g-masakhane
curl -H "X-Member-Id: m-thandi" -H "Idempotency-Key: $(uuidgen)" -H "Content-Type: application/json" \
  -d '{"amount":500,"currency":"ZAR"}' http://localhost:5106/api/groups/g-masakhane/contributions
```
Demo members: `m-thandi` (treasurer, Masakhane + Letsema), `m-kagiso` (limit R750), `m-bongani` (not in Masakhane: 403).

Web (Vite on 5173, proxies `/api` and `/health` to 5106):
```bash
cd web && npm ci && npm run dev
```

## Tests
```bash
dotnet test                                   # CI (needs NuGet)
cd web && npx vitest run && npm run build     # web
```
Offline workspace (no NuGet): `/home/claude/tools/run-xunit-offline.sh tests/Huddle.Tests src/Huddle.Api/Huddle.Api.csproj src/Huddle.SharedKernel/Huddle.SharedKernel.csproj src/Modules/*/Huddle.*.csproj`

## Configuration
| Key | Default | Meaning |
|---|---|---|
| `Auth:AllowDemoHeader` | false | allow the `X-Member-Id` dev identity outside Development (demos only) |
| `Seed:Demo` | false | seed demo data outside Development |

## Deploy
Not deployed yet. Slice 1 keeps everything in memory: a restart loses data. Do not deploy for real money before slice 2 (Postgres) and real sign-in.

## Monitor and alerts
- `/health/ready` unhealthy means the ledger integrity check failed: stop contribution traffic and page the on-call engineer.
- Meter `Huddle.Payments`: `huddle.contributions.posted|replayed|rejected{reason}`.

## Common incidents
- **Member says they paid twice:** look up their contributions in `/api/groups/{id}/contributions`; a retried request with the same key returns the same `contributionId` and posts nothing. Two different ids means two separate attempts (two keys).
- **409 idempotency-key-reused:** the client reused a key with a different amount; the web app makes a new key whenever the amount changes.
