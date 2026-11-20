# User flows

## Flow 1: a member contributes to their stokvel group (slice 1)
1. Open the app: `GET /api/me` picks the member's first group. Screen: `screens/stokvel-group.html` (loading state).
2. See the group: `GET /api/groups/{groupId}` (balance from ledger entries, month progress, members, my limit and wallet) and `GET /api/groups/{groupId}/contributions` (history, newest first). Screen: `stokvel-group.html` success or empty state. Non-member: 403 shown as an error panel.
3. Tap Contribute: `ContributeDialog` opens with a fresh idempotency key. Screen: `stokvel-group.html` dialog (idle).
4. Enter an amount and confirm: `POST /api/groups/{groupId}/contributions` with `Idempotency-Key`. Network drop: retried with the same key (dialog "retrying"). 422 shows the reason (invalid). 409 means the key was reused with another amount.
5. Success: 201 receipt (a replay returns the same receipt with `Idempotency-Replayed: true`); dialog "done"; the screen refetches balance and history.
