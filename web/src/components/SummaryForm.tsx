import { useState } from "react";
import { summarizePdf, summarizeText } from "../services/api";

interface SummaryFormProps {
  onResult: (summary: string) => void;
}

export function SummaryForm({ onResult }: SummaryFormProps) {
  const [text, setText] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      const summary = file ? await summarizePdf(file) : await summarizeText(text);
      onResult(summary);
    } catch {
      setError("Ocurrio un error al generar el resumen.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <form onSubmit={handleSubmit}>
      <div>
        <label htmlFor="text">Texto</label>
        <textarea
          id="text"
          rows={8}
          value={text}
          onChange={(e) => setText(e.target.value)}
          disabled={!!file}
          placeholder="Pega el texto que queres resumir..."
        />
      </div>

      <div>
        <label htmlFor="file">O subi un PDF</label>
        <input
          id="file"
          type="file"
          accept="application/pdf"
          onChange={(e) => setFile(e.target.files?.[0] ?? null)}
        />
      </div>

      <button type="submit" disabled={loading || (!text && !file)}>
        {loading ? "Resumiendo..." : "Resumir"}
      </button>

      {error && <p role="alert">{error}</p>}
    </form>
  );
}
