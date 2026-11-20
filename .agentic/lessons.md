# Lessons

Generic, reusable lessons only. No client names, data or secrets.

- Host the real minimal API on Kestrel port 0 in tests when WebApplicationFactory is unavailable: expose a `Build(args, configure)` method and read the bound address from `IServerAddressesFeature`.
- Store money as long minor units and expose a scale-2 decimal; then JSON shows `500.00` and arithmetic is exact.
- Idempotency: remember only successful results under a key, serialise concurrent requests per key, and fingerprint the canonical body (trailing zeros removed) so `500` and `500.00` match.
- Take per-account locks in a fixed (ordinal) order so multi-account postings can never deadlock.
- Use a no-break space as the thousands separator for currency so amounts never wrap on narrow screens.
