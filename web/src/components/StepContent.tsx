import { useRef, useState } from "react";
import { ACCEPTED_EXTENSIONS, MAX_TEXT_LENGTH } from "../constants";
import type { InputMode } from "../types";

const SOURCE_CONTEXT: Record<InputMode, { icon: string; label: string }> = {
  file: { icon: "📄", label: "Resumiendo un archivo" },
  url: { icon: "🔗", label: "Resumiendo un link" },
  text: { icon: "✏️", label: "Resumiendo un texto" },
};

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
      <div className="source-context">
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
          <p className="dz-title">{selectedFile ? selectedFile.name : "Arrastrá tu archivo"}</p>
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
          placeholder="Pegá el texto que querés resumir…"
          value={pastedText}
          maxLength={MAX_TEXT_LENGTH}
          onChange={(e) => onPastedTextChange(e.target.value)}
        />
      )}

      {inputMode === "url" && (
        <input
          type="url"
          className="url-input"
          placeholder="Pegá un link — artículo o YouTube"
          value={pastedUrl}
          onChange={(e) => onPastedUrlChange(e.target.value)}
        />
      )}
    </div>
  );
}
