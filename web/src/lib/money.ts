export interface Money {
  amount: number;
  currency: string;
}

const NBSP = ' ';
const grouping = new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/** Formats cents as rand, e.g. 1355000 -> "R13 550.00" (no-break space grouping so amounts never wrap). */
export function formatCents(cents: number, currency = 'ZAR'): string {
  const sign = cents < 0 ? '-' : '';
  const digits = grouping.format(Math.abs(cents) / 100).replace(/,/g, NBSP);
  return currency === 'ZAR' ? `${sign}R${digits}` : `${sign}${currency}${NBSP}${digits}`;
}

/** The API sends amounts with two decimals; convert to integer cents before doing anything with them. */
export function toCents(money: Money): number {
  return Math.round(money.amount * 100);
}

export function formatMoney(money: Money): string {
  return formatCents(toCents(money), money.currency);
}

export type ParsedAmount = { ok: true; cents: number } | { ok: false; reason: string };

/**
 * Parses what a person types into the amount box: "500", "R1 250.50", "1 250,50", "1,250.50".
 * Returns integer cents, or a plain-language reason.
 */
export function parseRandAmount(input: string): ParsedAmount {
  let text = input.replace(/[R\s  ]/gi, '');
  if (text === '') return { ok: false, reason: 'Enter an amount.' };
  if (text.includes(',') && !text.includes('.')) {
    const parts = text.split(',');
    text = parts.length === 2 && parts[1].length <= 2 ? `${parts[0]}.${parts[1]}` : parts.join('');
  } else {
    text = text.replace(/,/g, '');
  }
  if (!/^\d+(\.\d{1,2})?$/.test(text)) {
    return { ok: false, reason: 'Use numbers only, with at most two decimal places (cents).' };
  }
  const [whole, fraction = ''] = text.split('.');
  const cents = Number(whole) * 100 + Number(fraction.padEnd(2, '0'));
  if (!Number.isSafeInteger(cents)) return { ok: false, reason: 'That amount is too large.' };
  if (cents <= 0) return { ok: false, reason: 'A contribution must be more than R0.00.' };
  return { ok: true, cents };
}

/** Exact decimal string for the API body, e.g. 125050 -> 1250.5 as JSON number "1250.50". */
export function centsToAmount(cents: number): number {
  return Math.round(cents) / 100;
}
