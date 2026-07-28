import { buildPdfFilename, downloadAsPdf } from "../utils/download";
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
            <p className="name" title={sourceName}>{sourceName}</p>
            <p className="meta">{sourceMeta}</p>
          </div>
        </div>
      </div>

      <div className="summary-card">
        <div className="summary-card-content">
          <SummaryContent summary={summary} />
        </div>
      </div>

      <div className="done-actions">
        <button className="btn-soft" onClick={onReset}>
          Volver al inicio
        </button>
        <button className="btn-primary" onClick={() => void downloadAsPdf(summary, buildPdfFilename(sourceName))}>
          Descargar PDF
        </button>
      </div>
    </div>
  );
}
