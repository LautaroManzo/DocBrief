import type { InputMode, OutputLanguage, SummaryMode } from "../types";
import { Select } from "./Select";

const SUMMARY_MODE_OPTIONS = [
  { value: "basico", label: "Básico" },
  { value: "estudio", label: "Plan de estudio" },
];

const OUTPUT_LANGUAGE_OPTIONS = [
  { value: "es", label: "Español" },
  { value: "en", label: "English" },
];

const SOURCE_OPTIONS: { mode: InputMode; icon: string; label: string }[] = [
  { mode: "file", icon: "📄", label: "Archivo" },
  { mode: "url", icon: "🔗", label: "Link" },
  { mode: "text", icon: "✏️", label: "Texto" },
];

interface StepSourceProps {
  inputMode: InputMode;
  onInputModeChange: (mode: InputMode) => void;
  summaryMode: SummaryMode;
  onSummaryModeChange: (value: SummaryMode) => void;
  outputLanguage: OutputLanguage;
  onOutputLanguageChange: (value: OutputLanguage) => void;
}

export function StepSource({
  inputMode,
  onInputModeChange,
  summaryMode,
  onSummaryModeChange,
  outputLanguage,
  onOutputLanguageChange,
}: StepSourceProps) {
  return (
    <div className="view">
      <h1>
        ¿Qué querés <span style={{ color: "var(--accent)" }}>resumir</span>?
      </h1>

      <div className="source-cards">
        {SOURCE_OPTIONS.map((option) => (
          <div
            key={option.mode}
            className={`source-card${inputMode === option.mode ? " selected" : ""}`}
            onClick={() => onInputModeChange(option.mode)}
          >
            <span className="icon">{option.icon}</span>
            <span className="label">{option.label}</span>
          </div>
        ))}
      </div>

      <div className="options-row">
        <div className="field">
          <span>Tipo de resumen</span>
          <Select
            value={summaryMode}
            options={SUMMARY_MODE_OPTIONS}
            onChange={(v) => onSummaryModeChange(v as SummaryMode)}
          />
        </div>
        <div className="field">
          <span>Idioma de salida</span>
          <Select
            value={outputLanguage}
            options={OUTPUT_LANGUAGE_OPTIONS}
            onChange={(v) => onOutputLanguageChange(v as OutputLanguage)}
          />
        </div>
      </div>
    </div>
  );
}
