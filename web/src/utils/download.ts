import { jsPDF } from "jspdf";
import { parseSummaryMarkdown, type TextRun } from "./markdown";
import { isDarkTheme, renderMermaidSvg, svgToPng } from "./mermaidRender";

const MARGIN_LEFT = 56;
const MARGIN_RIGHT = 56;
const MARGIN_TOP = 64;
const MARGIN_BOTTOM = 64;

export function buildPdfFilename(sourceName: string): string {
  const withoutExtension = sourceName.replace(/\.[a-z0-9]{2,5}$/i, "");
  const slug = withoutExtension
    .normalize("NFD")
    .replace(new RegExp("[\\u0300-\\u036f]", "g"), "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 60);

  return `${slug || "resumen"}-resumen.pdf`;
}

export async function downloadAsPdf(content: string, filename: string) {
  // Mermaid inserta un elemento temporal en el body para medir el diagrama antes
  // de renderizarlo, lo que puede disparar un scroll fantasma por un instante.
  const previousBodyOverflow = document.body.style.overflow;
  document.body.style.overflow = "hidden";

  try {
    await generatePdf(content, filename);
  } finally {
    document.body.style.overflow = previousBodyOverflow;
  }
}

async function generatePdf(content: string, filename: string) {
  const doc = new jsPDF({ unit: "pt", format: "a4" });
  const pageWidth = doc.internal.pageSize.getWidth();
  const pageHeight = doc.internal.pageSize.getHeight();
  const maxWidth = pageWidth - MARGIN_LEFT - MARGIN_RIGHT;

  let y = MARGIN_TOP;

  function ensureSpace(lineHeight: number) {
    if (y + lineHeight > pageHeight - MARGIN_BOTTOM) {
      doc.addPage();
      y = MARGIN_TOP;
    }
  }

  function drawRuns(runs: TextRun[], x0: number, fontSize: number, lineHeight: number, availableWidth: number, forceBold = false) {
    doc.setFontSize(fontSize);

    const words = runs.flatMap((run) =>
      run.text.split(/\s+/).filter(Boolean).map((text) => ({ text, bold: forceBold || run.bold }))
    );

    let x = x0;
    let firstOnLine = true;

    for (const word of words) {
      doc.setFont("helvetica", word.bold ? "bold" : "normal");
      const wordWidth = doc.getTextWidth(word.text);
      const spaceWidth = doc.getTextWidth(" ");

      if (!firstOnLine && x + wordWidth > x0 + availableWidth) {
        y += lineHeight;
        ensureSpace(lineHeight);
        x = x0;
        firstOnLine = true;
      } else {
        ensureSpace(lineHeight);
      }

      if (!firstOnLine) x += spaceWidth;
      doc.text(word.text, x, y);
      x += wordWidth;
      firstOnLine = false;
    }

    y += lineHeight;
  }

  async function drawMermaidBlock(code: string) {
    try {
      const svg = await renderMermaidSvg(code, isDarkTheme());
      const { dataUrl, width, height } = await svgToPng(svg, 5);

      const availableHeight = pageHeight - y - MARGIN_BOTTOM;
      const scale = Math.min(maxWidth / width, availableHeight / height);
      const drawWidth = width * scale;
      const drawHeight = height * scale;
      const x = MARGIN_LEFT + (maxWidth - drawWidth) / 2;

      doc.addImage(dataUrl, "PNG", x, y, drawWidth, drawHeight);
      y += drawHeight + 12;
    } catch {
      // Si falla el render del diagrama, seguimos con el resto del PDF sin el.
    }
  }

  const blocks = parseSummaryMarkdown(content);

  for (let index = 0; index < blocks.length; index++) {
    const block = blocks[index];

    if (block.type === "mermaid") {
      if (block.code) await drawMermaidBlock(block.code);
    } else if (block.type === "heading") {
      const headingText = block.runs.map((run) => run.text).join("");
      const precedesMermaid = blocks[index + 1]?.type === "mermaid";
      const isQuestionsSection = /preguntas de repaso/i.test(headingText);

      if ((precedesMermaid || isQuestionsSection) && y > MARGIN_TOP) {
        doc.addPage();
        y = MARGIN_TOP;
      } else if (index > 0) {
        y += 8;
      }
      ensureSpace(20);
      drawRuns(block.runs, MARGIN_LEFT, 14, 18, maxWidth, true);
      y += 2;
    } else if (block.type === "bullet") {
      ensureSpace(15);
      drawRuns(block.runs, MARGIN_LEFT, 11, 15, maxWidth);
    } else {
      ensureSpace(15);
      drawRuns(block.runs, MARGIN_LEFT, 11, 15, maxWidth);
      y += 4;
    }
  }

  doc.save(filename);
}
