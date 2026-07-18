import { useEffect, useState } from "react";

function InfoIcon() {
  return (
    <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="12" cy="12" r="10" />
      <line x1="12" y1="11" x2="12" y2="16" />
      <circle cx="12" cy="8" r="0.5" fill="currentColor" stroke="none" />
    </svg>
  );
}

export function InfoButton() {
  const [open, setOpen] = useState(false);

  useEffect(() => {
    if (!open) return;

    function handleKeyDown(e: KeyboardEvent) {
      if (e.key === "Escape") setOpen(false);
    }
    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [open]);

  return (
    <>
      <button className="info-button" onClick={() => setOpen(true)} aria-label="Sobre esta app">
        <InfoIcon />
      </button>

      {open && (
        <div className="info-overlay" onClick={() => setOpen(false)}>
          <div className="info-modal" onClick={(e) => e.stopPropagation()}>
            <div className="info-modal-content">
              <div className="info-modal-header">
                <h2>Te lo resumo</h2>
                <button className="info-close" onClick={() => setOpen(false)} aria-label="Cerrar">
                  ✕
                </button>
              </div>

              <p>Una herramienta que convierte documentos largos en resúmenes claros usando IA.</p>

              <h3>¿Qué podés resumir?</h3>
              <ul>
                <li>Archivos PDF y Word</li>
                <li>Texto pegado directamente</li>
                <li>Links a artículos o páginas web</li>
                <li>Videos de YouTube</li>
              </ul>

              <h3>Tipos de resumen</h3>
              <ul>
                <li>
                  <strong>Básico</strong>: una síntesis clara y concisa de lo esencial.
                </li>
                <li>
                  <strong>Plan de estudio</strong>: material completo para aprender, con secciones
                  organizadas, términos clave y preguntas de repaso con respuestas.
                </li>
              </ul>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
