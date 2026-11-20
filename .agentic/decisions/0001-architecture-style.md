# 0001. Architecture style

- Status: Accepted
- Date: 2026-11-19

## Context

Stokvel groups manage contributions in notebooks and chat groups, so treasurers can't prove balances and members don't trust the totals. Launch traits: MoneyMovement, RegulatoryAudit, PersonalData, SeveralBusinessAreas, ExternalPartnerIntegrations, ComplexBusinessRules, SmallTeam. At global scale the advisor recommends ModularMonolith.

## Decision

Build a modular monolith (Accounts, Groups, Payments, Statements) with a double-entry append-only ledger, idempotency keys on every money command and an outbox for payment-rail calls. Evolution trigger: Stay a modular monolith (reference #9) until a module needs separate scaling or a separate regulatory boundary; consider event sourcing only if auditors require point-in-time replay beyond the ledger.

## Alternatives considered

- Microservices per business area: more moving parts and distributed transactions around money, with no scaling need.
- CRUD with a balance column: simplest, but balances drift and can't be proven to auditors or members.

## Consequences

- Module boundaries match the future services, so extraction is a deployment change, not a rewrite.
- One deployable and one database for the launch team to run.
