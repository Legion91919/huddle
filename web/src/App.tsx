import { useState } from 'react';
import { AppHeader, DEMO_MEMBERS } from './components/AppHeader';
import { GroupScreen } from './components/GroupScreen';

export function App() {
  const [memberId, setMemberId] = useState(DEMO_MEMBERS[0].id);
  // Bongani is not in Masakhane: viewing that group as him shows the 403 state.
  const groupId = memberId === 'm-bongani' ? 'g-masakhane' : undefined;
  return (
    <div className="wrap">
      <AppHeader memberId={memberId} onMemberChange={setMemberId} />
      <main>
        <GroupScreen key={memberId} memberId={memberId} groupId={groupId} />
      </main>
    </div>
  );
}
