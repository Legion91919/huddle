# Handover

What the client needs to own this without us.

- [ ] Source, infrastructure-as-code and pipelines in the client's own accounts
- [ ] Every secret rotated and owned by the client
- [ ] Architecture and decisions explained (`architecture.md`, `decisions/`)
- [ ] Runbook rehearsed by a client engineer
- [ ] Known issues and next steps listed below
- [ ] AI agent setup documented so their team can keep using it

## Known issues
- Slice 1 keeps all data in memory; a restart loses it.
- Identity is a development header (`X-Member-Id`); the API refuses to start outside Development without `Auth:AllowDemoHeader=true`.
- OpenTelemetry exporter not wired yet (package feed blocked in the build workspace); metrics use the built-in Meter.

## Recommended next steps
- Slice 2 persistence, then real sign-in, then statements and payouts (see `backlog.md`).