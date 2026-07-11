import { downloadAsPdf } from "../utils/download";
import { SummaryContent } from "./SummaryContent";

interface DoneViewProps {
  summary: string;
  sourceName: string;
  sourceMeta: string;
  originalMinutes: number;
  summaryMinutes: number | string;
  timeSavedLabel: string;
  onReset: () => void;
}

export function DoneView({
  summary,
  sourceName,
  sourceMeta,
  originalMinutes,
  summaryMinutes,
  timeSavedLabel,
  onReset,
}: DoneViewProps) {
  return (
    <div className="done-view">
      <div className="done-header">
        <div className="done-header-file">
          <div className="done-header-icon" />
          <div>
            <p className="name">{sourceName}</p>
            <p className="meta">{sourceMeta}</p>
          </div>
        </div>
        <button className="reset-link desktop-only" onClick={onReset}>
          Cargar otro documento
        </button>
      </div>

      <div className="chips">
        <span className="chip">
          <span className="chip-text">📖 {originalMinutes} min de lectura normal</span>
          <span className="chip-mobile">
            <span className="chip-mobile-label">Lectura normal</span>
            <span className="chip-mobile-value">{originalMinutes} min</span>
          </span>
        </span>
        <span className="chip">
          <span className="chip-text">⚡ {summaryMinutes} min con el resumen</span>
          <span className="chip-mobile">
            <span className="chip-mobile-label">Con el resumen</span>
            <span className="chip-mobile-value">{summaryMinutes} min</span>
          </span>
        </span>
        <span className="chip accent">Ahorrás {timeSavedLabel}</span>
      </div>

      <div className="summary-card">
        <SummaryContent summary={summary} />
      </div>

      <div className="done-actions">
        <button className="btn-primary" onClick={() => downloadAsPdf(summary, "resumen-docbrief.pdf")}>
          Descargar PDF
        </button>
      </div>

      <button className="reset-link mobile-only" onClick={onReset}>
        Cargar otro documento
      </button>
    </div>
  );
}
