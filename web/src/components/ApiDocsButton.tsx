const API_URL = import.meta.env.VITE_API_URL;

function CodeIcon() {
  return (
    <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <polyline points="8 6 2 12 8 18" />
      <polyline points="16 6 22 12 16 18" />
    </svg>
  );
}

export function ApiDocsButton() {
  return (
    <a
      className="info-button"
      href={`${API_URL}/swagger`}
      target="_blank"
      rel="noopener noreferrer"
      aria-label="Documentación de la API"
    >
      <CodeIcon />
    </a>
  );
}
