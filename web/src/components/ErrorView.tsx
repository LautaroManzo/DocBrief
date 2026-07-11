interface ErrorViewProps {
  message: string;
  onRetry: () => void;
}

export function ErrorView({ message, onRetry }: ErrorViewProps) {
  return (
    <div className="view">
      <div className="error-icon">
        <span>!</span>
      </div>
      <div>
        <h1 style={{ fontSize: 22 }}>No pudimos generar el resumen</h1>
        <p className="error-message">{message}</p>
      </div>
      <button className="btn-primary" onClick={onRetry}>
        Reintentar
      </button>
    </div>
  );
}
