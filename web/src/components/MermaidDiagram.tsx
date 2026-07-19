import { useEffect, useRef, useState } from "react";
import { isDarkTheme, renderMermaidSvg } from "../utils/mermaidRender";

interface MermaidDiagramProps {
  code: string;
}

export function MermaidDiagram({ code }: MermaidDiagramProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [error, setError] = useState(false);

  useEffect(() => {
    let cancelled = false;

    async function render() {
      try {
        const svg = await renderMermaidSvg(code, isDarkTheme());
        if (!cancelled && containerRef.current) {
          containerRef.current.innerHTML = svg;
        }
      } catch {
        if (!cancelled) setError(true);
      }
    }

    render();
    return () => {
      cancelled = true;
    };
  }, [code]);

  if (error) return null;

  return <div className="mermaid-diagram" ref={containerRef} />;
}
