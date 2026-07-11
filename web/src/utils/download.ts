import { jsPDF } from "jspdf";
import { parseSummaryMarkdown, type TextRun } from "./markdown";

const MARGIN_LEFT = 56;
const MARGIN_RIGHT = 56;
const MARGIN_TOP = 64;
const MARGIN_BOTTOM = 64;

export function downloadAsPdf(content: string, filename: string) {
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

  const blocks = parseSummaryMarkdown(content);

  blocks.forEach((block, index) => {
    if (block.type === "heading") {
      if (index > 0) y += 8;
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
  });

  doc.save(filename);
}
