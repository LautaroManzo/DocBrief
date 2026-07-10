const API_URL = import.meta.env.VITE_API_URL;

export async function summarizeText(text: string): Promise<string> {
  const response = await fetch(`${API_URL}/api/summary/text`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ text }),
  });

  if (!response.ok) {
    throw new Error("No se pudo generar el resumen.");
  }

  const data = await response.json();
  return data.summary;
}

export async function summarizePdf(file: File): Promise<string> {
  const formData = new FormData();
  formData.append("file", file);

  const response = await fetch(`${API_URL}/api/summary/pdf`, {
    method: "POST",
    body: formData,
  });

  if (!response.ok) {
    throw new Error("No se pudo generar el resumen.");
  }

  const data = await response.json();
  return data.summary;
}
