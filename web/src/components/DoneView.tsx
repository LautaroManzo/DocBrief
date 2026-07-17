import { downloadAsPdf } from "../utils/download";
import { SummaryContent } from "./SummaryContent";

interface DoneViewProps {
  summary: string;
  sourceName: string;
  sourceMeta: string;
  onReset: () => void;
}

export function DoneView({ summary, sourceName, sourceMeta, onReset }: DoneViewProps) {
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
