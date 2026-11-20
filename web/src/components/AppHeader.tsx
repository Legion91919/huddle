import { Logo } from './Logo';

export const DEMO_MEMBERS = [
  { id: 'm-thandi', name: 'Thandi Mokoena (treasurer)' },
  { id: 'm-kagiso', name: 'Kagiso Molefe (member)' },
  { id: 'm-bongani', name: 'Bongani Sithole (other group)' },
];

/** Header with the logo and, until real sign-in exists, a development "viewing as" picker. */
export function AppHeader({ memberId, onMemberChange }: { memberId: string; onMemberChange: (id: string) => void }) {
  return (
    <header className="app-header">
      <div className="brand">
        <Logo /> Huddle
      </div>
      <label className="view-as">
        Viewing as
        <select value={memberId} onChange={(e) => onMemberChange(e.target.value)}>
          {DEMO_MEMBERS.map((m) => (
            <option key={m.id} value={m.id}>
              {m.name}
            </option>
          ))}
        </select>
      </label>
    </header>
  );
}
