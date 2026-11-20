import { useEffect, useRef, useState } from 'react';
import { contributeWithRetry, newIdempotencyKey, type RetryOptions } from '../api/contribute';
import type { FetchLike } from '../api/client';
import type { ContributionReceipt, GroupResponse } from '../api/types';
import { formatCents, parseRandAmount, toCents } from '../lib/money';

type Phase =
  | { name: 'idle' }
  | { name: 'sending'; attempt: number }
  | { name: 'rejected'; message: string }
  | { name: 'unreachable' }
  | { name: 'done'; receipt: ContributionReceipt };

export interface ContributeDialogProps {
  group: GroupResponse;
  memberId: string;
  onClose: () => void;
  onDone: (receipt: ContributionReceipt) => void;
  fetchImpl?: FetchLike;
  retry?: RetryOptions;
  makeKey?: () => string;
}

/**
 * Amount entry and submit. One idempotency key per attempt: a new key when the dialog opens or the amount changes;
 * automatic retries and "Try again" after a network failure reuse it, so a contribution can never be posted twice.
 */
export function ContributeDialog({ group, memberId, onClose, onDone, fetchImpl, retry, makeKey = newIdempotencyKey }: ContributeDialogProps) {
  const [text, setText] = useState(() => (toCents(group.monthlyContribution) / 100).toFixed(2));
  const [key, setKey] = useState(makeKey);
  const [phase, setPhase] = useState<Phase>({ name: 'idle' });
  const inputRef = useRef<HTMLInputElement>(null);
  const limitCents = toCents(group.me.contributionLimit);
  const parsed = parseRandAmount(text);
  const clientError = !parsed.ok ? parsed.reason : parsed.cents > limitCents ? `${formatCents(parsed.cents)} is above your per-contribution limit of ${formatCents(limitCents)}.` : null;
  const busy = phase.name === 'sending';

  useEffect(() => inputRef.current?.focus(), []);
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && !busy && onClose();
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [busy, onClose]);

  function changeAmount(value: string) {
    setText(value);
    setKey(makeKey()); // a different amount is a different attempt
    if (phase.name !== 'sending') setPhase({ name: 'idle' });
  }

  async function submit() {
    if (!parsed.ok || clientError) return;
    setPhase({ name: 'sending', attempt: 1 });
    const outcome = await contributeWithRetry(
      { groupId: group.groupId, memberId, cents: parsed.cents, currency: group.currency, idempotencyKey: key },
      fetchImpl,
      { ...retry, onRetry: (attempt) => setPhase({ name: 'sending', attempt }) },
    );
    if (outcome.kind === 'posted') {
      setPhase({ name: 'done', receipt: outcome.receipt });
      onDone(outcome.receipt);
    } else if (outcome.kind === 'rejected') {
      setPhase({ name: 'rejected', message: outcome.problem.detail ?? outcome.problem.title ?? 'The contribution was not accepted.' });
    } else {
      setPhase({ name: 'unreachable' });
    }
  }

  return (
    <div className="dialog-backdrop" onClick={(e) => e.target === e.currentTarget && !busy && onClose()}>
      <div className="dialog" role="dialog" aria-modal="true" aria-labelledby="contribute-title">
        <h2 id="contribute-title">Contribute to {group.name}</h2>
        {phase.name === 'done' ? (
          <>
            <div className="alert ok" role="status">
              <b>{formatCents(toCents(phase.receipt.amount))} contributed.</b> Thank you. The group balance is updating.
            </div>
            <div className="actions">
              <button className="primary" onClick={onClose}>
                Close
              </button>
            </div>
          </>
        ) : (
          <form
            onSubmit={(e) => {
              e.preventDefault();
              void submit();
            }}
          >
            <div className="field">
              <label htmlFor="amount">Amount</label>
              <div className="in">
                <span aria-hidden="true">R</span>
                <input id="amount" ref={inputRef} inputMode="decimal" autoComplete="off" value={text} disabled={busy} aria-invalid={clientError ? true : undefined} aria-describedby="amount-hint" onChange={(e) => changeAmount(e.target.value)} />
              </div>
              <p id="amount-hint" className={clientError ? 'hint err' : 'hint'}>
                {clientError ?? `Up to ${formatCents(limitCents)} per contribution · wallet ${formatCents(toCents(group.me.walletBalance))}`}
              </p>
            </div>
            <div className="chips" role="group" aria-label="Quick amounts">
              {[50000, 75000, 100000].filter((c) => c <= limitCents).map((c) => (
                <button type="button" key={c} disabled={busy} onClick={() => changeAmount((c / 100).toFixed(2))}>
                  {formatCents(c)}
                </button>
              ))}
            </div>
            <div aria-live="polite">
              {phase.name === 'sending' && (
                <p className={phase.attempt > 1 ? 'alert info' : 'hint'}>
                  {phase.attempt > 1 ? `Connection dropped. Retrying safely with the same reference (attempt ${phase.attempt}) — you will not be charged twice.` : 'Sending… please keep this open.'}
                </p>
              )}
              {phase.name === 'rejected' && (
                <div className="alert err" role="alert">
                  {phase.message}
                </div>
              )}
              {phase.name === 'unreachable' && (
                <div className="alert err" role="alert">
                  We could not reach Huddle to confirm your contribution. Nothing will be taken twice: tap Try again to check with the same reference.
                </div>
              )}
            </div>
            <p className="key">Reference: {key}</p>
            <div className="actions">
              <button type="button" onClick={onClose} disabled={busy}>
                Cancel
              </button>
              <button type="submit" className="primary" disabled={busy || clientError !== null}>
                {phase.name === 'unreachable' ? 'Try again' : parsed.ok ? `Contribute ${formatCents(parsed.cents)}` : 'Contribute'}
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}
