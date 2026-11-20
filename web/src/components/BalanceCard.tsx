import type { GroupResponse } from '../api/types';
import { Amount } from './Amount';

export function BalanceCard({ group, onContribute }: { group: GroupResponse; onContribute: () => void }) {
  const { thisMonth, me } = group;
  const pct = thisMonth.membersTotal === 0 ? 0 : Math.round((thisMonth.membersPaid / thisMonth.membersTotal) * 100);
  const empty = group.contributionCount === 0;
  return (
    <section className="card" aria-labelledby="group-name">
      <div className="label" id="group-name">
        {group.name} · group balance
      </div>
      <div className="balance" data-testid="group-balance">
        <Amount money={group.balance} />
      </div>
      {!empty && (
        <>
          <div className="progress" role="progressbar" aria-valuenow={thisMonth.membersPaid} aria-valuemin={0} aria-valuemax={thisMonth.membersTotal} aria-label={`Members paid in ${thisMonth.label}`}>
            <span style={{ width: `${pct}%` }} />
          </div>
          <p className="label" style={{ margin: 'var(--space-sm) 0 0' }}>
            {thisMonth.label}: {thisMonth.membersPaid} of {thisMonth.membersTotal} paid · <Amount money={thisMonth.collected} /> collected · {group.schedule.toLowerCase()}
          </p>
        </>
      )}
      {me.paidThisMonth ? (
        <div className="paid-note">
          <span className="ok-text">✓ You have paid for {thisMonth.label.split(' ')[0]}.</span>
          <button onClick={onContribute}>Contribute again</button>
        </div>
      ) : (
        <div className="nudge">
          <p>
            <b>Your {thisMonth.label.split(' ')[0]} contribution is due.</b>
            <br />
            <Amount money={group.monthlyContribution} /> · limit <Amount money={me.contributionLimit} /> · wallet <Amount money={me.walletBalance} />
          </p>
          <button className="primary" onClick={onContribute}>
            Contribute
          </button>
        </div>
      )}
    </section>
  );
}
