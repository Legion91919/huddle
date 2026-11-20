export function ErrorPanel({ title, detail, onRetry }: { title: string; detail: string; onRetry?: () => void }) {
  return (
    <section className="card">
      <div className="alert err" role="alert">
        <b>{title}</b>
        <p style={{ margin: '4px 0 0' }}>{detail}</p>
      </div>
      {onRetry && (
        <p>
          <button className="primary" onClick={onRetry}>
            Try again
          </button>
        </p>
      )}
    </section>
  );
}
