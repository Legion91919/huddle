import type { Money } from '../lib/money';

export type MemberRole = 'member' | 'treasurer' | 'chairperson';

export interface MeResponse {
  memberId: string;
  displayName: string;
  groups: { groupId: string; name: string }[];
}

export interface MemberResponse {
  memberId: string;
  displayName: string;
  initials: string;
  role: MemberRole;
  totalContributed: Money;
  contributionCount: number;
  lastContributionAt: string | null;
  paidThisMonth: boolean;
}

export interface GroupResponse {
  groupId: string;
  name: string;
  currency: string;
  schedule: string;
  monthlyContribution: Money;
  balance: Money;
  contributionCount: number;
  thisMonth: { label: string; membersPaid: number; membersTotal: number; collected: Money };
  me: { memberId: string; displayName: string; role: MemberRole; contributionLimit: Money; walletBalance: Money; paidThisMonth: boolean };
  members: MemberResponse[];
}

export interface HistoryItem {
  postingId: string;
  reference: string;
  memberId: string;
  memberName: string;
  amount: Money;
  occurredAt: string;
  runningBalance: Money;
}

export interface HistoryResponse {
  groupId: string;
  balance: Money;
  total: number;
  items: HistoryItem[];
}

export interface ContributionReceipt {
  contributionId: string;
  postingId: string;
  groupId: string;
  memberId: string;
  amount: Money;
  postedAt: string;
}

/** RFC 9457 problem details as the API sends them. */
export interface Problem {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  reason?: string;
  errors?: Record<string, string[]>;
}
