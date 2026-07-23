interface ProcessingViewProps {
  title: string;
}

export function ProcessingView({ title }: ProcessingViewProps) {
  return (
    <div className="view">
      <div className="spinner">
        <div className="spinner-track" />
        <div className="spinner-arc" />
      </div>
      <h1 style={{ fontSize: 18 }}>{title}</h1>
    </div>
  );
}
