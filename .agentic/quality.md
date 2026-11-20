# Quality

## Definition of done
- Acceptance criteria met and demonstrated, one test per criterion.
- Ledger invariants hold in property tests (fixed seeds) and in the readiness check.
- Unit tests for rules, HTTP tests for each endpoint, architecture tests for module boundaries, vitest for web logic.
- CI green: build (warnings as errors), tests, format, web build + vitest, secret scan, privacy guard.
- Docs in `.agentic/` and `docs/DESIGN.md` updated in the same change.

## Service levels
| SLI | SLO | Measured by |
|---|---|---|
| API success rate (non-5xx) | 99.9% over 30 days | ASP.NET Core request metrics |
| p95 latency: group read, contribute | < 300 ms | request duration histogram |
| Ledger integrity (every posting sums to zero, balances = entries) | 100%, always | `/health/ready` every 30 s |
| Duplicate payments from retries | 0 | replayed count vs postings per key (test-enforced) |
| Contribution rejections by reason | watched, no SLO | `huddle.contributions.rejected{reason}` |

## Error budget policy
Contribute 5xx burning budget 10x faster than allowed for 5 minutes pages on-call; ledger integrity failure pages immediately.
