# Next step

- **Doing:** nothing in progress; slice 1 complete.
- **Done so far:** Sketch leg (tokens, variants A/B/C, decision, final screen with four states); slice 1 API + web; 66 offline .NET tests and 22 vitest tests green; design dossier `docs/DESIGN.md`.
- **Next action:** start slice 2: add `Huddle.Accounts` EF Core `LedgerDbContext` (schema `accounts`, tables `postings`, `entries`, no balance column) behind `ILedger`; first test: the property tests run against it with Testcontainers Postgres.
- **Blocked by:** NuGet access for EF Core and Testcontainers (blocked in the build workspace; fine in CI).
