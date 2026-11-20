import type { HistoryItem } from '../api/types';
import { Amount } from './Amount';

const dateFmt = new Intl.DateTimeFormat('en-GB', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'Africa/Johannesburg' });
const timeFmt = new Intl.DateTimeFormat('en-GB', { hour: '2-digit', minute: '2-digit', timeZone: 'Africa/Johannesburg' });

export function HistoryTable({ items, total, onContribute }: { items: HistoryItem[]; total: number; onContribute: () => void }) {
  if (items.length === 0) {
    return (
      <section className="card empty">
        <h2>No contributions yet</h2>
        <p className="muted">When members contribute, every rand shows up here with the balance after it.</p>
        <button className="primary" onClick={onContribute}>
          Make the first contribution
        </button>
      </section>
    );
  }
  return (
    <section className="card">
      <table>
        <caption>
          Contribution history <span className="muted" style={{ fontWeight: 400 }}>· {total}</span>
        </caption>
        <thead>
          <tr>
            <th scope="col">Date</th>
            <th scope="col">Member</th>
            <th scope="col" className="num">
              Amount
            </th>
            <th scope="col" className="num hide-sm">
              Balance after
            </th>
          </tr>
        </thead>
        <tbody>
          {items.map((item) => {
            const at = new Date(item.occurredAt);
            return (
              <tr key={item.postingId}>
                <td>
                  {dateFmt.format(at)}
                  <span className="hide-sm muted">, {timeFmt.format(at)}</span>
                </td>
                <td>{item.memberName}</td>
                <td className="num">
                  <Amount money={item.amount} />
                </td>
                <td className="num hide-sm">
                  <Amount money={item.runningBalance} />
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </section>
  );
}
