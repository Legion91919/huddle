# Product brief

> **Showcase project.** Thrive Mutual (fictional), a financial cooperative for stokvel savings groups is a fictional client and every name, user and number here is invented.
> Huddle is an original digital banking app, built to show how the Baton model handles a consumer-scale product.

## Client
Thrive Mutual (fictional), a financial cooperative for stokvel savings groups

## Problem
Stokvel groups manage contributions in notebooks and chat groups, so treasurers can't prove balances and members don't trust the totals.

## Users
Group members (weekly, mobile); treasurers (weekly, need statements); compliance (monthly).

## Outcomes
Every balance provable from ledger entries; a retried payment never pays twice; monthly group statement generated in one click.

## Constraints
Money as decimal in cents; no balance column, only ledger entries; every money request carries an idempotency key; KYC before first payout.

## Out of scope (first release)
Anything not needed for the outcomes above in the first three slices.

## Traits (for the architecture advisor)
- MoneyMovement: "accounts, contributions, transfers and payouts"
- RegulatoryAudit: "auditors and members need full history"
- PersonalData: "identity documents, phone numbers, bank details"
- SeveralBusinessAreas: "accounts, groups, payments, statements"
- ExternalPartnerIntegrations: "payment rails and identity verification"
- ComplexBusinessRules: "group payout rules, contribution schedules, limits"
- SmallTeam: "five engineers"

Global-scale profile used for the evolution plan: MoneyMovement, RegulatoryAudit, PersonalData, SeveralBusinessAreas, ExternalPartnerIntegrations, ComplexBusinessRules.
