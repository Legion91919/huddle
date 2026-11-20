# Backlog

Vertical slices, smallest valuable first.

## Now
- [x] Sketch leg: tokens + three variants of the slice-1 screen (`sketch-ui`) → `ux/decision.md`, final `ux/screens/stokvel-group.html`
- [x] Slice 1: A member contributes to their stokvel group and everyone sees the group balance calculated from ledger entries
  - [x] Given a contribution with an idempotency key, when sent twice, then one pair of ledger entries exists (`Criterion1_*` module + HTTP tests, parallel same-key test)
  - [x] Given ledger entries, then every account balance equals the sum of its entries and all entries sum to zero (property tests, `Criterion2_*`)
  - [x] Given a member of group G, then they see G's balance and contribution history; non-members get 403 (`Criterion3_*`)
  - [x] Given an amount <= 0 or above the member's limit, then it's rejected with a clear reason (`Criterion4_*`, ProblemDetails `reason`)
  - Test notes: one test per criterion; seeded invented data.

## Next
- [ ] Slice 2: persistence (Postgres via EF Core) behind the same repository interfaces; idempotency keys in the same transaction as the posting, 24 h expiry
- [ ] CI green with all gates (first push of the repo)
- [ ] Real sign-in (OIDC) replacing the development identity header
- [ ] Playwright e2e for flow 1 with axe

## Later
- [ ] Monthly group statement (one click, PDF) for treasurers
- [ ] Payouts to members via payment rails adapter + outbox; KYC before first payout
