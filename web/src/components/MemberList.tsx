import type { MemberResponse } from '../api/types';

const ROLE: Record<MemberResponse['role'], string> = { member: 'Member', treasurer: 'Treasurer', chairperson: 'Chairperson' };

export function MemberList({ members, monthLabel }: { members: MemberResponse[]; monthLabel: string }) {
  const month = monthLabel.split(' ')[0].slice(0, 3);
  return (
    <section className="card" aria-labelledby="members-title">
      <h2 id="members-title">Members ({members.length})</h2>
      {members.length === 0 ? (
        <p className="muted">No members yet.</p>
      ) : (
        <ul className="members">
          {members.map((m) => (
            <li key={m.memberId}>
              <span className="avatar" aria-hidden="true">
                {m.initials}
              </span>
              <span>
                {m.displayName}
                <small>
                  {ROLE[m.role]} · {m.paidThisMonth ? <span className="ok-text">✓ paid {month}</span> : <span>not yet paid {month}</span>}
                </small>
              </span>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
