export interface TextRun {
  text: string;
  bold: boolean;
}

export interface Block {
  type: "heading" | "paragraph" | "bullet";
  runs: TextRun[];
}

export function parseSummaryMarkdown(markdown: string): Block[] {
  const blocks: Block[] = [];
  let paragraphLines: string[] = [];

  function flushParagraph() {
    if (paragraphLines.length > 0) {
      const text = paragraphLines.join(" ").trim();
      if (text) blocks.push({ type: "paragraph", runs: parseInline(text) });
      paragraphLines = [];
    }
  }

  for (const rawLine of markdown.split("\n")) {
    const line = rawLine.trim();
    if (line === "") {
      flushParagraph();
      continue;
    }

    const headingMatch = line.match(/^#{1,6}\s+(.*)/);
    if (headingMatch) {
      flushParagraph();
      blocks.push({ type: "heading", runs: parseInline(headingMatch[1]) });
      continue;
    }

    const bulletMatch = line.match(/^[-*]\s+(.*)/);
    if (bulletMatch) {
      flushParagraph();
      blocks.push({ type: "bullet", runs: parseInline(bulletMatch[1]) });
      continue;
    }

    paragraphLines.push(line);
  }
  flushParagraph();

  return blocks;
}

function parseInline(text: string): TextRun[] {
  const runs: TextRun[] = [];
  const regex = /\*\*(.+?)\*\*/g;
  let lastIndex = 0;
  let match: RegExpExecArray | null;

  while ((match = regex.exec(text)) !== null) {
    if (match.index > lastIndex) {
      runs.push({ text: text.slice(lastIndex, match.index), bold: false });
    }
    runs.push({ text: match[1], bold: true });
    lastIndex = match.index + match[0].length;
  }
  if (lastIndex < text.length) {
    runs.push({ text: text.slice(lastIndex), bold: false });
  }

  return runs.length > 0 ? runs : [{ text, bold: false }];
}
