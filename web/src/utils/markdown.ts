export interface TextRun {
  text: string;
  bold: boolean;
}

export interface Block {
  type: "heading" | "paragraph" | "bullet" | "mermaid";
  runs: TextRun[];
  code?: string;
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

  const lines = markdown.split("\n");

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i].trim();
    if (line === "") {
      flushParagraph();
      continue;
    }

    const fenceMatch = line.match(/^```(\w*)/);
    if (fenceMatch) {
      flushParagraph();
      const isMermaid = fenceMatch[1].toLowerCase() === "mermaid";
      const codeLines: string[] = [];
      i++;
      while (i < lines.length && !lines[i].trim().startsWith("```")) {
        codeLines.push(lines[i]);
        i++;
      }
      if (isMermaid) {
        blocks.push({ type: "mermaid", runs: [], code: codeLines.join("\n").trim() });
      }
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
