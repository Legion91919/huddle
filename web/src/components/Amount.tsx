import { formatMoney, type Money } from '../lib/money';

export function Amount({ money, className }: { money: Money; className?: string }) {
  return <span className={['num', className].filter(Boolean).join(' ')}>{formatMoney(money)}</span>;
}
