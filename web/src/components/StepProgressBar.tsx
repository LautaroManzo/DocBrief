interface StepProgressBarProps {
  step: 1 | 2 | 3;
  progress: number;
}

export function StepProgressBar({ step, progress }: StepProgressBarProps) {
  // step2 termina en 38% ((2-1)*33+5) — el tramo de "processing" arranca ahi
  // mismo para que la barra no retroceda al pasar de step2 a processing.
  const width = step === 3 ? 38 + progress * 0.62 : (step - 1) * 33 + 5;

  return (
    <div className="progress-track">
      <div className="progress-fill" style={{ width: `${width}%` }} />
    </div>
  );
}
