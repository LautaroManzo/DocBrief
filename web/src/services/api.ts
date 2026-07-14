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

async function assertOk(response: Response) {
  if (response.ok) return;

  if (response.status === 429) {
    throw new Error("Alcanzaste el limite de resumenes permitidos. Esperá un momento y volvé a intentar.");
  }

  if (response.status === 400) {
    const text = await response.text();
    let parsed: unknown;
    try {
      parsed = JSON.parse(text);
    } catch {
      parsed = undefined;
    }

    if (typeof parsed === "string" && parsed.trim()) {
      throw new Error(parsed);
    }
  }

  throw new Error("No se pudo generar el resumen.");
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

  await assertOk(response);

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

  await assertOk(response);

  return response.json();
}

export async function summarizeUrl(url: string, options: SummaryOptions): Promise<SummaryResponse> {
  const response = await fetch(`${API_URL}/api/summary/url`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      url,
      summaryLength: options.summaryLength,
      outputLanguage: options.outputLanguage,
    }),
  });

  await assertOk(response);

  return response.json();
}
