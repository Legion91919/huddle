# AGENTS.md

Huddle: Stokvel groups manage contributions in notebooks and chat groups, so treasurers can't prove balances and members don't trust the totals.

## Read first
Everything about this project lives in the hidden `.agentic/` folder.
Start with `.agentic/README.md`; it says which file answers which question.
Load only the files your task needs.

## Hard rules
- Work in thin vertical slices: one ticket from `.agentic/backlog.md` at a time.
- No change without a test that fails first. Run the full test suite before you finish.
- A decision that is hard to reverse gets an ADR in `.agentic/decisions/`.
- Never commit secrets, real client data or personal data. Use `.env.example`.
- Do not grow this file. New knowledge goes in the right `.agentic/` file; history goes in `.agentic/changelog.md`.
- When you learn something that would help on the next project, add it to `.agentic/lessons.md`.

## What is live
- Slice 1: contribute to a stokvel group; balance = sum of append-only ledger entries; idempotent `POST /api/groups/{id}/contributions`.
- Modules: Accounts, Groups, Payments, Statements (`src/Modules/*`), contracts in `src/Huddle.SharedKernel`. API on port 5106, web in `web/`.
- In-memory stores only; Postgres is slice 2. Design: `docs/DESIGN.md`.
