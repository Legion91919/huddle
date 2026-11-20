import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { GroupResponse } from '../api/types';
import { ContributeDialog } from './ContributeDialog';

const zar = (amount: number) => ({ amount, currency: 'ZAR' });
const group: GroupResponse = {
  groupId: 'g-masakhane',
  name: 'Masakhane Savings Circle',
  currency: 'ZAR',
  schedule: 'Monthly, by the 25th',
  monthlyContribution: zar(500),
  balance: zar(13550),
  contributionCount: 27,
  thisMonth: { label: 'November 2026', membersPaid: 3, membersTotal: 6, collected: zar(1500) },
  me: { memberId: 'm-thandi', displayName: 'Thandi Mokoena', role: 'treasurer', contributionLimit: zar(2000), walletBalance: zar(5400), paidThisMonth: false },
  members: [],
};

afterEach(cleanup);

function keysOf(fetchImpl: ReturnType<typeof vi.fn>) {
  return fetchImpl.mock.calls.map(([, init]) => (init.headers as Record<string, string>)['Idempotency-Key']);
}

describe('ContributeDialog', () => {
  it('blocks amounts above the limit before calling the API', () => {
    const fetchImpl = vi.fn();
    render(<ContributeDialog group={group} memberId="m-thandi" onClose={() => {}} onDone={() => {}} fetchImpl={fetchImpl} />);

    fireEvent.change(screen.getByLabelText('Amount'), { target: { value: '2500' } });

    expect(screen.getByText(/above your per-contribution limit/)).toBeTruthy();
    expect((screen.getByRole('button', { name: 'Contribute R2 500.00' }) as HTMLButtonElement).disabled).toBe(true);
    expect(fetchImpl).not.toHaveBeenCalled();
  });

  it('reuses one key across automatic retries and "Try again", and uses a new key for a new amount', async () => {
    let n = 0;
    const makeKey = () => `key-${++n}`;
    const fetchImpl = vi.fn().mockRejectedValue(new TypeError('offline'));
    const onDone = vi.fn();
    render(<ContributeDialog group={group} memberId="m-thandi" onClose={() => {}} onDone={onDone} fetchImpl={fetchImpl} retry={{ sleep: () => Promise.resolve() }} makeKey={makeKey} />);

    fireEvent.click(screen.getByRole('button', { name: 'Contribute R500.00' }));
    await screen.findByText(/could not reach Huddle/);
    expect(keysOf(fetchImpl)).toEqual(['key-1', 'key-1', 'key-1']);

    const receipt = { contributionId: 'c1', postingId: 'p1', groupId: 'g-masakhane', memberId: 'm-thandi', amount: zar(500), postedAt: '2026-11-19T08:30:00Z' };
    fetchImpl.mockReset().mockResolvedValue(new Response(JSON.stringify(receipt), { status: 201 }));
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }));
    await waitFor(() => expect(onDone).toHaveBeenCalledWith(receipt));
    expect(keysOf(fetchImpl)).toEqual(['key-1']);
    expect(screen.getByRole('status').textContent).toContain('R500.00 contributed');
  });

  it('shows the server reason when a contribution is rejected', async () => {
    const problem = { status: 422, reason: 'insufficient-funds', detail: 'Not enough money in your wallet. Available balance is R120.00.' };
    const fetchImpl = vi.fn().mockResolvedValue(new Response(JSON.stringify(problem), { status: 422 }));
    render(<ContributeDialog group={group} memberId="m-thandi" onClose={() => {}} onDone={() => {}} fetchImpl={fetchImpl} />);

    fireEvent.change(screen.getByLabelText('Amount'), { target: { value: '750' } });
    fireEvent.click(screen.getByRole('button', { name: 'Contribute R750.00' }));

    expect((await screen.findByRole('alert')).textContent).toContain('Available balance is R120.00');
  });
});
