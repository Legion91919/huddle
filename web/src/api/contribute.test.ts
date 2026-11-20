import { describe, expect, it, vi } from 'vitest';
import { contributeWithRetry, type ContributeInput } from './contribute';

const input: ContributeInput = { groupId: 'g-masakhane', memberId: 'm-thandi', cents: 50000, currency: 'ZAR', idempotencyKey: 'key-123' };
const receipt = { contributionId: 'c1', postingId: 'p1', groupId: 'g-masakhane', memberId: 'm-thandi', amount: { amount: 500, currency: 'ZAR' }, postedAt: '2026-11-19T08:30:00Z' };
const noSleep = { sleep: () => Promise.resolve() };

function json(status: number, body: unknown, headers: Record<string, string> = {}) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json', ...headers } });
}

describe('contributeWithRetry', () => {
  it('retries network failures with the same idempotency key and returns the receipt', async () => {
    const fetchImpl = vi
      .fn()
      .mockRejectedValueOnce(new TypeError('Failed to fetch'))
      .mockResolvedValueOnce(json(503, { title: 'busy' }))
      .mockResolvedValueOnce(json(201, receipt, { 'Idempotency-Replayed': 'true' }));

    const outcome = await contributeWithRetry(input, fetchImpl, noSleep);

    expect(outcome).toEqual({ kind: 'posted', receipt, replayed: true });
    expect(fetchImpl).toHaveBeenCalledTimes(3);
    const keys = fetchImpl.mock.calls.map(([, init]) => (init.headers as Record<string, string>)['Idempotency-Key']);
    expect(new Set(keys)).toEqual(new Set(['key-123']));
    expect(JSON.parse(fetchImpl.mock.calls[0][1].body)).toEqual({ amount: 500, currency: 'ZAR' });
  });

  it('never retries a 4xx answer and surfaces the ProblemDetails reason', async () => {
    const problem = { status: 422, reason: 'amount-above-limit', detail: 'R2 500.00 is above your per-contribution limit of R2 000.00.' };
    const fetchImpl = vi.fn().mockResolvedValue(json(422, problem));

    const outcome = await contributeWithRetry(input, fetchImpl, noSleep);

    expect(outcome).toEqual({ kind: 'rejected', status: 422, problem });
    expect(fetchImpl).toHaveBeenCalledTimes(1);
  });

  it('gives up after the configured attempts and reports unreachable', async () => {
    const fetchImpl = vi.fn().mockRejectedValue(new TypeError('offline'));
    const onRetry = vi.fn();

    const outcome = await contributeWithRetry(input, fetchImpl, { ...noSleep, attempts: 3, onRetry });

    expect(outcome).toEqual({ kind: 'unreachable', attempts: 3 });
    expect(fetchImpl).toHaveBeenCalledTimes(3);
    expect(onRetry).toHaveBeenCalledWith(2);
    expect(onRetry).toHaveBeenCalledWith(3);
  });
});
