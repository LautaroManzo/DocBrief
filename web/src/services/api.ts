import type { OutputLanguage, SummaryMode } from "../types";

const API_URL = import.meta.env.VITE_API_URL;

interface SummaryOptions {
  summaryMode: SummaryMode;
  outputLanguage: OutputLanguage;
  includeConceptMap?: boolean;
}

export interface SummaryResponse {
  summary: string;
  sourceTitle?: string;
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
      summaryMode: options.summaryMode,
      outputLanguage: options.outputLanguage,
      includeConceptMap: options.includeConceptMap ?? false,
    }),
  });

  await assertOk(response);

  return response.json();
}

export async function summarizeFile(file: File, options: SummaryOptions): Promise<SummaryResponse> {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("summaryMode", options.summaryMode);
  formData.append("outputLanguage", options.outputLanguage);
  formData.append("includeConceptMap", String(options.includeConceptMap ?? false));

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
      summaryMode: options.summaryMode,
      outputLanguage: options.outputLanguage,
      includeConceptMap: options.includeConceptMap ?? false,
    }),
  });

  await assertOk(response);

  return response.json();
}
