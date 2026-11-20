/** Huddle mark: three people gathered above a cradling arc. Original artwork, token colours only. */
export function Logo({ size = 28 }: { size?: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 32 32" aria-hidden="true">
      <circle cx="16" cy="16" r="15" fill="var(--color-accent)" />
      <circle cx="9.5" cy="14" r="3.6" fill="#fff" />
      <circle cx="22.5" cy="14" r="3.6" fill="#fff" />
      <circle cx="16" cy="9.5" r="3.6" fill="#fff" />
      <path d="M8 20c2.5 4 5 5.5 8 5.5s5.5-1.5 8-5.5" fill="none" stroke="var(--color-brand-mark)" strokeWidth="3" strokeLinecap="round" />
    </svg>
  );
}
