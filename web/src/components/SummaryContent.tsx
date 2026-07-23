import { Fragment } from "react";
import { parseSummaryMarkdown, type TextRun } from "../utils/markdown";
import { ConceptMapSection } from "./ConceptMapSection";

interface SummaryContentProps {
  summary: string;
}

function Runs({ runs }: { runs: TextRun[] }) {
  return (
    <>
      {runs.map((run, i) =>
        run.bold ? <strong key={i}>{run.text}</strong> : <Fragment key={i}>{run.text}</Fragment>
      )}
    </>
  );
}

export function SummaryContent({ summary }: SummaryContentProps) {
  const blocks = parseSummaryMarkdown(summary);
  const elements: React.ReactNode[] = [];
  let bulletBuffer: TextRun[][] = [];

  function flushBullets(key: string) {
    if (bulletBuffer.length === 0) return;
    elements.push(
      <ul key={key}>
        {bulletBuffer.map((runs, i) => (
          <li key={i}>
            <Runs runs={runs} />
          </li>
        ))}
      </ul>
    );
    bulletBuffer = [];
  }

  let i = 0;
  while (i < blocks.length) {
    const block = blocks[i];

    if (block.type === "bullet") {
      bulletBuffer.push(block.runs);
      i++;
      continue;
    }

    flushBullets(`ul-${i}`);

    if (block.type === "heading" && blocks[i + 1]?.type === "mermaid") {
      elements.push(
        <ConceptMapSection key={i} headingRuns={block.runs} code={blocks[i + 1].code ?? ""} />
      );
      i += 2;
      continue;
    }

    if (block.type === "mermaid") {
      // Bloque mermaid sin titulo previo (no deberia pasar, pero por las dudas lo ignoramos).
    } else if (block.type === "heading") {
      elements.push(
        <h3 key={i}>
          <Runs runs={block.runs} />
        </h3>
      );
    } else {
      elements.push(
        <p key={i}>
          <Runs runs={block.runs} />
        </p>
      );
    }

    i++;
  }
  flushBullets("ul-end");

  return <>{elements}</>;
}
