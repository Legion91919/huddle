import { describe, expect, it } from 'vitest';
import { centsToAmount, formatCents, formatMoney, parseRandAmount } from './money';

const NBSP = ' ';

describe('formatCents', () => {
  it('formats rand with no-break space grouping and two decimals', () => {
    expect(formatCents(1355000)).toBe(`R13${NBSP}550.00`);
    expect(formatCents(50)).toBe('R0.50');
    expect(formatCents(-125050)).toBe(`-R1${NBSP}250.50`);
  });

  it('formats API money without floating point drift', () => {
    expect(formatMoney({ amount: 0.1 + 0.2, currency: 'ZAR' })).toBe('R0.30');
  });
});

describe('parseRandAmount', () => {
  it.each([
    ['500', 50000],
    ['R1 250.50', 125050],
    [`R1${NBSP}250.5`, 125050],
    ['1 250,50', 125050],
    ['1,250.50', 125050],
    ['0.01', 1],
  ])('parses %s as %i cents', (input, cents) => {
    expect(parseRandAmount(input)).toEqual({ ok: true, cents });
  });

  it.each(['', '0', '0.00', '-5', '10.001', 'abc', '1.2.3'])('rejects %s with a reason', (input) => {
    const result = parseRandAmount(input);
    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.reason.length).toBeGreaterThan(5);
  });

  it('round-trips cents to the API amount exactly', () => {
    expect(centsToAmount(125050)).toBe(1250.5);
    expect(JSON.stringify({ amount: centsToAmount(19999) })).toBe('{"amount":199.99}');
  });
});
