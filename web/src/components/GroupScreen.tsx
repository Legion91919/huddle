import { useCallback, useEffect, useState } from 'react';
import { ApiError, getJson } from '../api/client';
import type { GroupResponse, HistoryResponse, MeResponse } from '../api/types';
import { BalanceCard } from './BalanceCard';
import { ContributeDialog } from './ContributeDialog';
import { ErrorPanel } from './ErrorPanel';
import { HistoryTable } from './HistoryTable';
import { MemberList } from './MemberList';

type State =
  | { status: 'loading' }
  | { status: 'error'; title: string; detail: string; retry: boolean }
  | { status: 'ready'; group: GroupResponse; history: HistoryResponse };

function describe(error: unknown): { title: string; detail: string; retry: boolean } {
  if (error instanceof ApiError && error.status === 403) return { title: 'This group is private to its members.', detail: error.message, retry: false };
  if (error instanceof ApiError && error.status === 404) return { title: 'Group not found.', detail: error.message, retry: false };
  return { title: 'We could not load your group.', detail: 'Your money is safe; this is a connection problem. Try again in a moment.', retry: true };
}

export function GroupScreen({ memberId, groupId }: { memberId: string; groupId?: string }) {
  const [state, setState] = useState<State>({ status: 'loading' });
  const [dialogOpen, setDialogOpen] = useState(false);

  const load = useCallback(async () => {
    setState((s) => (s.status === 'ready' ? s : { status: 'loading' }));
    try {
      let id = groupId;
      if (!id) {
        const me = await getJson<MeResponse>('/api/me', memberId);
        id = me.groups[0]?.groupId;
        if (!id) {
          setState({ status: 'error', title: 'You are not in a group yet.', detail: 'Ask your treasurer to add you.', retry: false });
          return;
        }
      }
      const [group, history] = await Promise.all([
        getJson<GroupResponse>(`/api/groups/${encodeURIComponent(id)}`, memberId),
        getJson<HistoryResponse>(`/api/groups/${encodeURIComponent(id)}/contributions`, memberId),
      ]);
      setState({ status: 'ready', group, history });
    } catch (error) {
      setState({ status: 'error', ...describe(error) });
    }
  }, [groupId, memberId]);

  useEffect(() => {
    setState({ status: 'loading' });
    void load();
  }, [load]);

  if (state.status === 'loading') {
    return (
      <div aria-busy="true" aria-label="Loading group">
        <div className="card">
          <div className="skel" style={{ width: '40%' }} />
          <div className="skel" style={{ height: 36, width: '60%' }} />
          <div className="skel" />
        </div>
        <div className="card">
          <div className="skel" />
          <div className="skel" />
          <div className="skel" style={{ width: '70%' }} />
        </div>
      </div>
    );
  }
  if (state.status === 'error') {
    return <ErrorPanel title={state.title} detail={state.detail} onRetry={state.retry ? () => void load() : undefined} />;
  }

  const { group, history } = state;
  return (
    <>
      <BalanceCard group={group} onContribute={() => setDialogOpen(true)} />
      <MemberList members={group.members} monthLabel={group.thisMonth.label} />
      <HistoryTable items={history.items} total={history.total} onContribute={() => setDialogOpen(true)} />
      {dialogOpen && <ContributeDialog group={group} memberId={memberId} onClose={() => setDialogOpen(false)} onDone={() => void load()} />}
    </>
  );
}
