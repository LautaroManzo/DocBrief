import { useRef, useState } from "react";
import { ACCEPTED_EXTENSIONS, MAX_TEXT_LENGTH } from "../constants";
import type { InputMode } from "../types";

function PlayIcon() {
  return (
    <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="2" y="5" width="20" height="14" rx="4" />
      <path d="M10.5 9.5l4.5 2.5-4.5 2.5v-5z" fill="currentColor" stroke="none" />
    </svg>
  );
}

function ArticleIcon() {
  return (
    <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M6 3h9l4 4v14a1 1 0 0 1-1 1H6a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1z" />
      <path d="M14 3v5h5" />
      <line x1="8" y1="13" x2="16" y2="13" />
      <line x1="8" y1="17" x2="13" y2="17" />
    </svg>
  );
}

function BlogIcon() {
  return (
    <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="12" cy="12" r="9" />
      <path d="M3 12h18" />
      <path d="M12 3a14 14 0 0 1 0 18 14 14 0 0 1 0-18z" />
    </svg>
  );
}

function UploadIcon() {
  return (
    <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M12 15V3" />
      <path d="M7 8l5-5 5 5" />
      <path d="M4 17v3a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1v-3" />
    </svg>
  );
}

function LinkIcon() {
  return (
    <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M9.5 14.5l5-5" />
      <path d="M11 6.5l1.5-1.5a3.5 3.5 0 0 1 5 5L16 11.5" />
      <path d="M13 17.5L11.5 19a3.5 3.5 0 0 1-5-5L8 12.5" />
    </svg>
  );
}

function PencilIcon() {
  return (
    <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M12 20h9" />
      <path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z" />
    </svg>
  );
}

const SOURCE_CONTEXT: Record<InputMode, { icon: React.ReactNode; label: string }> = {
  file: { icon: <UploadIcon />, label: "Subí o arrastrá tu archivo" },
  url: { icon: <LinkIcon />, label: "Pegá la URL del sitio o video" },
  text: { icon: <PencilIcon />, label: "Pegá o escribí el texto" },
};

const URL_SOURCE_TYPES = [
  { icon: <PlayIcon />, label: "YouTube" },
  { icon: <ArticleIcon />, label: "Artículo" },
  { icon: <BlogIcon />, label: "Blog" },
];

interface StepContentProps {
  inputMode: InputMode;
  selectedFile: File | null;
  onSelectedFileChange: (file: File | null) => void;
  pastedText: string;
  onPastedTextChange: (value: string) => void;
  pastedUrl: string;
  onPastedUrlChange: (value: string) => void;
}

export function StepContent({
  inputMode,
  selectedFile,
  onSelectedFileChange,
  pastedText,
  onPastedTextChange,
  pastedUrl,
  onPastedUrlChange,
}: StepContentProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [isDragOver, setIsDragOver] = useState(false);
  const context = SOURCE_CONTEXT[inputMode];

  function handleDrop(e: React.DragEvent) {
    e.preventDefault();
    setIsDragOver(false);
    const file = e.dataTransfer.files[0];
    if (file) onSelectedFileChange(file);
  }

  return (
    <div className="view">
      <div className="source-context source-context-title">
        <span className="icon">{context.icon}</span>
        <span className="label">{context.label}</span>
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
          <p className="dz-title">{selectedFile ? selectedFile.name : "Hacé clic o soltalo acá"}</p>
          <p className="dz-subtitle">PDF, DOCX — máx. 10 MB</p>
          <input
            ref={fileInputRef}
            type="file"
            accept={ACCEPTED_EXTENSIONS.join(",")}
            onChange={(e) => onSelectedFileChange(e.target.files?.[0] ?? null)}
          />
        </div>
      )}

      {inputMode === "text" && (
        <textarea
          placeholder="Ej: apuntes de clase, un capítulo, un artículo largo…"
          value={pastedText}
          maxLength={MAX_TEXT_LENGTH}
          onChange={(e) => onPastedTextChange(e.target.value)}
        />
      )}

      {inputMode === "url" && (
        <>
          <input
            type="url"
            className="url-input"
            placeholder="www.ejemplo.com/articulo"
            value={pastedUrl}
            onChange={(e) => onPastedUrlChange(e.target.value)}
          />
          <div className="source-pills">
            {URL_SOURCE_TYPES.map((type) => (
              <span key={type.label} className="source-pill">
                <span className="icon">{type.icon}</span>
                {type.label}
              </span>
            ))}
          </div>
        </>
      )}
    </div>
  );
}
