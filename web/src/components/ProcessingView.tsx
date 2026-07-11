interface ProcessingViewProps {
  title: string;
  subtitle: string;
  progress: number;
}

export function ProcessingView({ title, subtitle, progress }: ProcessingViewProps) {
  return (
    <div className="view">
      <div className="spinner">
        <div className="spinner-track" />
        <div className="spinner-arc" />
        <div className="spinner-core">
          <div />
        </div>
      </div>
      <div>
        <h1 style={{ fontSize: 24 }}>{title}</h1>
        <p className="subtitle">{subtitle}</p>
      </div>
      <div className="progress-track">
        <div className="progress-fill" style={{ width: `${progress}%` }} />
      </div>
    </div>
  );
}
