# Components (stokvel group screen)

Build order for `web/src/components/`. Every data component handles loading, empty, error and success.

| Component | Props | Loading | Empty | Error | Success |
|---|---|---|---|---|---|
| `AppHeader` | `memberName`, `viewAs`, `onViewAs` | name placeholder | n/a | n/a | logo mark, "Viewing as" picker (dev identity) |
| `GroupScreen` | `groupId`, `memberId` | skeleton cards | balance R0.00 + empty history | `ErrorPanel` (403 says "not a member", others "try again") | composes the parts below |
| `BalanceCard` | `group: GroupResponse`, `onContribute` | skeleton bar | R0.00, no progress bar | handled by parent | balance, month progress bar, due nudge when `me.paidThisMonth` is false |
| `MemberList` | `members: MemberResponse[]` | 3 skeleton rows | "No members yet" | handled by parent | initials avatar, name, role, "✓ paid Nov" or "due" |
| `HistoryTable` | `items: HistoryItem[]`, `total` | skeleton rows | "No contributions yet" + first-contribution button | handled by parent | date, member, amount, balance after (right-aligned, tabular) |
| `ContributeDialog` | `open`, `group`, `onClose`, `onDone(receipt)` | n/a | n/a | inline alert with the ProblemDetails `detail`; network failure after 3 tries says nothing was taken twice | "R500.00 contributed" status |
| `ErrorPanel` | `title`, `detail`, `onRetry?` | n/a | n/a | role=alert text + retry | n/a |
| `Amount` | `money: {amount, currency}` | n/a | n/a | n/a | "R13 550.00" with tabular numbers and a no-break space |

Dialog internals: one idempotency key per user attempt (`crypto.randomUUID()`), created when the dialog opens or the amount changes; network errors and 502/503/504 are retried up to 3 times with the same key and backoff; 4xx are shown, never retried.
