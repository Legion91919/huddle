import { centsToAmount } from '../lib/money';
import { memberHeaders, readProblem, type FetchLike } from './client';
import type { ContributionReceipt, Problem } from './types';

export interface ContributeInput {
  groupId: string;
  memberId: string;
  cents: number;
  currency: string;
  /** One key per user attempt. Every automatic retry of that attempt reuses it, so the server posts at most once. */
  idempotencyKey: string;
}

export type ContributeOutcome =
  | { kind: 'posted'; receipt: ContributionReceipt; replayed: boolean }
  | { kind: 'rejected'; status: number; problem: Problem }
  | { kind: 'unreachable'; attempts: number };

export interface RetryOptions {
  attempts?: number;
  delaysMs?: number[];
  sleep?: (ms: number) => Promise<void>;
  onRetry?: (attempt: number) => void;
}

const RETRYABLE_STATUS = new Set([502, 503, 504]);

export function newIdempotencyKey(): string {
  return globalThis.crypto.randomUUID();
}

/**
 * Sends a contribution. Network errors and gateway errors are retried with the SAME Idempotency-Key and backoff;
 * a repeat that already succeeded on the server just returns the original receipt. 4xx answers are never retried.
 */
export async function contributeWithRetry(input: ContributeInput, fetchImpl: FetchLike = fetch, options: RetryOptions = {}): Promise<ContributeOutcome> {
  const attempts = options.attempts ?? 3;
  const delays = options.delaysMs ?? [400, 1200, 3000];
  const sleep = options.sleep ?? ((ms: number) => new Promise<void>((resolve) => setTimeout(resolve, ms)));
  const body = JSON.stringify({ amount: centsToAmount(input.cents), currency: input.currency });

  for (let attempt = 1; attempt <= attempts; attempt++) {
    if (attempt > 1) {
      options.onRetry?.(attempt);
      await sleep(delays[Math.min(attempt - 2, delays.length - 1)]);
    }
    let response: Response;
    try {
      response = await fetchImpl(`/api/groups/${encodeURIComponent(input.groupId)}/contributions`, {
        method: 'POST',
        headers: { ...memberHeaders(input.memberId), 'Content-Type': 'application/json', 'Idempotency-Key': input.idempotencyKey },
        body,
      });
    } catch {
      continue; // network failure: retry with the same key
    }
    if (response.ok) {
      return { kind: 'posted', receipt: (await response.json()) as ContributionReceipt, replayed: response.headers.get('Idempotency-Replayed') === 'true' };
    }
    if (RETRYABLE_STATUS.has(response.status)) continue;
    return { kind: 'rejected', status: response.status, problem: await readProblem(response) };
  }
  return { kind: 'unreachable', attempts };
}
