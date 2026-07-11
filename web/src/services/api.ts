import type { OutputLanguage, SummaryLength } from "../types";

const API_URL = import.meta.env.VITE_API_URL;

interface SummaryOptions {
  summaryLength: SummaryLength;
  outputLanguage: OutputLanguage;
}

export interface SummaryResponse {
  summary: string;
  originalWordCount: number;
}

export async function summarizeText(text: string, options: SummaryOptions): Promise<SummaryResponse> {
  const response = await fetch(`${API_URL}/api/summary/text`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      text,
      summaryLength: options.summaryLength,
      outputLanguage: options.outputLanguage,
    }),
  });

  if (!response.ok) {
    throw new Error("No se pudo generar el resumen.");
  }

  return response.json();
}

export async function summarizeFile(file: File, options: SummaryOptions): Promise<SummaryResponse> {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("summaryLength", options.summaryLength);
  formData.append("outputLanguage", options.outputLanguage);

  const response = await fetch(`${API_URL}/api/summary/file`, {
    method: "POST",
    body: formData,
  });

  if (!response.ok) {
    throw new Error("No se pudo generar el resumen.");
  }

  return response.json();
}
