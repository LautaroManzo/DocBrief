interface SummaryResultProps {
  summary: string;
}

export function SummaryResult({ summary }: SummaryResultProps) {
  if (!summary) return null;

  return (
    <div>
      <h2>Resumen</h2>
      <p style={{ whiteSpace: "pre-wrap" }}>{summary}</p>
    </div>
  );
}
