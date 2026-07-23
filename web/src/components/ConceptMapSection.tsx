import { useEffect, useState } from "react";
import type { TextRun } from "../utils/markdown";
import { isDarkTheme, renderMermaidSvg } from "../utils/mermaidRender";

interface ConceptMapSectionProps {
  headingRuns: TextRun[];
  code: string;
}

// Muestra el titulo y el diagrama juntos, o nada, si el modelo no genero un
// mapa conceptual valido (bloque vacio o mermaid invalido que falla al renderizar).
export function ConceptMapSection({ headingRuns, code }: ConceptMapSectionProps) {
  const [svg, setSvg] = useState<string | null>(null);
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    let cancelled = false;

    if (!code.trim()) {
      setFailed(true);
      return;
    }

    renderMermaidSvg(code, isDarkTheme())
      .then((result) => {
        if (!cancelled) setSvg(result);
      })
      .catch(() => {
        if (!cancelled) setFailed(true);
      });

    return () => {
      cancelled = true;
    };
  }, [code]);

  if (failed) return null;
  if (!svg) return null;

  return (
    <>
      <h3>
        {headingRuns.map((run, i) =>
          run.bold ? <strong key={i}>{run.text}</strong> : <span key={i}>{run.text}</span>
        )}
      </h3>
      <div className="mermaid-diagram" dangerouslySetInnerHTML={{ __html: svg }} />
    </>
  );
}
