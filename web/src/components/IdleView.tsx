import { useRef, useState } from "react";
import type { InputMode, OutputLanguage, SummaryLength } from "../types";
import { Select } from "./Select";

const SUMMARY_LENGTH_OPTIONS = [
  { value: "corto", label: "Corto" },
  { value: "medio", label: "Medio" },
  { value: "detallado", label: "Detallado" },
];

const OUTPUT_LANGUAGE_OPTIONS = [
  { value: "es", label: "Español" },
  { value: "en", label: "English" },
];

interface IdleViewProps {
  inputMode: InputMode;
  onInputModeChange: (mode: InputMode) => void;
  summaryLength: SummaryLength;
  onSummaryLengthChange: (value: SummaryLength) => void;
  outputLanguage: OutputLanguage;
  onOutputLanguageChange: (value: OutputLanguage) => void;
  pastedText: string;
  onPastedTextChange: (value: string) => void;
  pastedUrl: string;
  onPastedUrlChange: (value: string) => void;
  onSubmitFile: (file: File) => void;
  onSubmitText: () => void;
  onSubmitUrl: () => void;
}

const MAX_FILE_SIZE = 10 * 1024 * 1024;
const ACCEPTED_EXTENSIONS = [".pdf", ".docx"];
const MAX_TEXT_LENGTH = 10_000;

export function IdleView({
  inputMode,
  onInputModeChange,
  summaryLength,
  onSummaryLengthChange,
  outputLanguage,
  onOutputLanguageChange,
  pastedText,
  onPastedTextChange,
  pastedUrl,
  onPastedUrlChange,
  onSubmitFile,
  onSubmitText,
  onSubmitUrl,
}: IdleViewProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [isDragOver, setIsDragOver] = useState(false);

  function handleFile(file: File | undefined) {
    if (!file) return;
    onSubmitFile(file);
  }

  function handleDrop(e: React.DragEvent) {
    e.preventDefault();
    setIsDragOver(false);
    handleFile(e.dataTransfer.files[0]);
  }

  return (
    <div className="view">
      <div>
        <h1>Documentos largos, resúmenes cortos</h1>
        <p className="subtitle">Todo lo importante, al instante.</p>
      </div>

      <div className="tabs">
        <button className={inputMode === "file" ? "active" : ""} onClick={() => onInputModeChange("file")}>
          Subir archivo
        </button>
        <button className={inputMode === "url" ? "active" : ""} onClick={() => onInputModeChange("url")}>
          Pegar link
        </button>
        <button className={inputMode === "text" ? "active" : ""} onClick={() => onInputModeChange("text")}>
          Pegar texto
        </button>
      </div>

      <div className="options-row">
        <div className="field">
          <span>Largo del resumen</span>
          <Select
            value={summaryLength}
            options={SUMMARY_LENGTH_OPTIONS}
            onChange={(v) => onSummaryLengthChange(v as SummaryLength)}
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

      {inputMode === "file" && (
        <div
          className={`dropzone${isDragOver ? " dragover" : ""}`}
          onClick={() => fileInputRef.current?.click()}
          onDragOver={(e) => {
            e.preventDefault();
            setIsDragOver(true);
          }}
          onDragLeave={() => setIsDragOver(false)}
          onDrop={handleDrop}
        >
          <div className="dropzone-icon">
            <div className="arrow" />
          </div>
          <p className="dz-title">Arrastrá tu archivo acá</p>
          <p className="dz-subtitle">
            o <span className="dz-link">elegí un archivo</span> — PDF, DOCX (máx. 10 MB)
          </p>
          <input
            ref={fileInputRef}
            type="file"
            accept={ACCEPTED_EXTENSIONS.join(",")}
            onChange={(e) => handleFile(e.target.files?.[0])}
          />
        </div>
      )}

      {inputMode === "text" && (
        <div style={{ width: "100%", maxWidth: 560, display: "flex", flexDirection: "column", gap: 6 }}>
          <textarea
            placeholder="Pegá o escribí el texto que querés resumir…"
            value={pastedText}
            maxLength={MAX_TEXT_LENGTH}
            onChange={(e) => onPastedTextChange(e.target.value)}
          />
          <span className="char-counter">
            {pastedText.length.toLocaleString("es")} / {MAX_TEXT_LENGTH.toLocaleString("es")}
          </span>
          <button className="btn-primary" style={{ alignSelf: "center" }} onClick={onSubmitText}>
            Resumir texto
          </button>
        </div>
      )}

      {inputMode === "url" && (
        <div style={{ width: "100%", maxWidth: 560, display: "flex", flexDirection: "column", gap: 12 }}>
          <input
            type="url"
            className="url-input"
            placeholder="https://ejemplo.com/articulo"
            value={pastedUrl}
            onChange={(e) => onPastedUrlChange(e.target.value)}
          />
          <button className="btn-primary" style={{ alignSelf: "center" }} onClick={onSubmitUrl}>
            Resumir link
          </button>
        </div>
      )}
    </div>
  );
}

export { MAX_FILE_SIZE, ACCEPTED_EXTENSIONS, MAX_TEXT_LENGTH };
