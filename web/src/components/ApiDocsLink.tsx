const API_URL = import.meta.env.VITE_API_URL;

export function ApiDocsLink() {
  return (
    <a className="api-docs-link" href={`${API_URL}/swagger`} target="_blank" rel="noopener noreferrer">
      <span className="api-docs-icon">{"</>"}</span>
      API docs
    </a>
  );
}
