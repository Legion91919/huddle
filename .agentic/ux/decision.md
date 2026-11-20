# Decision: stokvel group screen

Variants (all at 390px and 1440px, headless browser screenshots):
- `screens/stokvel-group-a.html` A, table-first: stats strip, members table and full ledger table side by side, keyboard shortcuts.
- `screens/stokvel-group-b.html` B, summary-first: big balance card with month progress, one Contribute button, avatar strip, recent list.
- `screens/stokvel-group-c.html` C, workflow-first: "Your November contribution" guided step with quick amounts, queue of who still has to pay, history collapsed.

| Criterion (1-5) | A table | B summary | C workflow |
|---|---|---|---|
| Main job speed (see balance, contribute) | 3 | 4 | 5 |
| Clarity of numbers | 5 | 4 | 3 |
| Accessibility (390px, touch targets, reading order) | 2 | 5 | 4 |
| Fits tone (calm, trustworthy) | 3 | 5 | 3 |
| **Total** | **13** | **18** | **15** |

Notes: A is great for a treasurer on a laptop but cramped on a phone and visually heavy. C makes paying fast, but the bright task panel and "who still has to pay" queue feel like pressure and hide the history that builds trust.

## Pick: B, with two borrowings
- From C: an inline "Your November contribution is due" nudge with the Contribute button, shown only when I have not paid this month.
- From A: the history as a real table with right-aligned tabular amounts and a "Balance after" column (hidden under 480px).
- Contribute opens a dialog: amount field (R prefix, right-aligned), quick amounts, limit and wallet shown up front, plain-language retry message.

Final: `screens/stokvel-group.html` (state switcher: loading, empty, error, success; dialog states: idle, invalid, sending, retrying, failed, done).

## Contrast (WCAG AA, computed from design-tokens.json)
text/bg 13.48, text/surface 14.92, muted/bg 5.79, muted/surface 6.41, muted/surface-sunk 5.35, accent/surface 6.44, on-accent/accent 6.44,
accent-strong/accent-soft 7.87, success/success-soft 5.17, warning/warning-soft 5.56, danger/danger-soft 5.59. Brand-mark ochre (4.28) is used only in the logo graphic (non-text, 3:1 applies).
Paid/due status always pairs colour with a symbol and words ("✓ paid Nov", "due by 25 Nov").
