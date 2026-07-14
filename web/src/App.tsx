import { useRef, useState } from "react";
import { ApiDocsLink } from "./components/ApiDocsLink";
import { DoneView } from "./components/DoneView";
import { ErrorView } from "./components/ErrorView";
import { ACCEPTED_EXTENSIONS, IdleView, MAX_FILE_SIZE, MAX_TEXT_LENGTH } from "./components/IdleView";
import { ProcessingView } from "./components/ProcessingView";
import { ThemeToggle } from "./components/ThemeToggle";
import { summarizeFile, summarizeText, summarizeUrl } from "./services/api";
import type { InputMode, OutputLanguage, Phase, SummaryLength } from "./types";

const WORDS_PER_MINUTE = 200;

function App() {
  const [phase, setPhase] = useState<Phase>("idle");
  const [inputMode, setInputMode] = useState<InputMode>("file");
  const [summaryLength, setSummaryLength] = useState<SummaryLength>("medio");
  const [outputLanguage, setOutputLanguage] = useState<OutputLanguage>("es");
  const [pastedText, setPastedText] = useState("");
  const [pastedUrl, setPastedUrl] = useState("");
  const [progress, setProgress] = useState(0);
  const [errorMessage, setErrorMessage] = useState("");

  const [summary, setSummary] = useState("");
  const [originalWordCount, setOriginalWordCount] = useState(0);
  const [sourceName, setSourceName] = useState("");
  const [sourceMeta, setSourceMeta] = useState("");

  const progressTimer = useRef<ReturnType<typeof setInterval> | null>(null);

  function startProgressAnimation() {
    setProgress(0);
    progressTimer.current = setInterval(() => {
      setProgress((p) => (p < 90 ? p + 3 : p));
    }, 150);
  }

  function stopProgressAnimation() {
    if (progressTimer.current) {
      clearInterval(progressTimer.current);
      progressTimer.current = null;
    }
  }

  function handleError(error: unknown) {
    stopProgressAnimation();
    setErrorMessage(error instanceof Error ? error.message : "Ocurrió un error al conectar con el servidor. Intentá nuevamente.");
    setPhase("error");
  }

  async function handleSubmitFile(file: File) {
    const extension = "." + file.name.split(".").pop()?.toLowerCase();

    if (file.size > MAX_FILE_SIZE || !ACCEPTED_EXTENSIONS.includes(extension)) {
      setErrorMessage("El archivo pesa más de 10 MB o el formato no es compatible. Probá con un PDF o DOCX de menor tamaño.");
      setPhase("error");
      return;
    }

    setSourceName(file.name);
    setSourceMeta(`${(file.size / (1024 * 1024)).toFixed(1)} MB`);
    setPhase("processing");
    startProgressAnimation();

    try {
      const result = await summarizeFile(file, { summaryLength, outputLanguage });
      stopProgressAnimation();
      setProgress(100);
      setSummary(result.summary);
      setOriginalWordCount(result.originalWordCount);
      setPhase("done");
    } catch (error) {
      handleError(error);
    }
  }

  async function handleSubmitText() {
    if (!pastedText.trim()) {
      setErrorMessage("No encontramos texto para resumir. Pegá o escribí contenido antes de continuar.");
      setPhase("error");
      return;
    }

    if (pastedText.length > MAX_TEXT_LENGTH) {
      setErrorMessage(`El texto supera el límite de ${MAX_TEXT_LENGTH.toLocaleString("es")} caracteres.`);
      setPhase("error");
      return;
    }

    setSourceName("Texto pegado");
    setSourceMeta(`${pastedText.length} caracteres`);
    setPhase("processing");
    startProgressAnimation();

    try {
      const result = await summarizeText(pastedText, { summaryLength, outputLanguage });
      stopProgressAnimation();
      setProgress(100);
      setSummary(result.summary);
      setOriginalWordCount(result.originalWordCount);
      setPhase("done");
    } catch (error) {
      handleError(error);
    }
  }

  async function handleSubmitUrl() {
    if (!pastedUrl.trim()) {
      setErrorMessage("Pegá una URL valida antes de continuar.");
      setPhase("error");
      return;
    }

    setSourceName(pastedUrl);
    setSourceMeta("Pagina web");
    setPhase("processing");
    startProgressAnimation();

    try {
      const result = await summarizeUrl(pastedUrl, { summaryLength, outputLanguage });
      stopProgressAnimation();
      setProgress(100);
      setSummary(result.summary);
      setOriginalWordCount(result.originalWordCount);
      setPhase("done");
    } catch (error) {
      handleError(error);
    }
  }

  function reset() {
    setPhase("idle");
    setPastedText("");
    setPastedUrl("");
    setSummary("");
    setProgress(0);
  }

  function retryFromError() {
    setPhase("idle");
  }

  const originalMinutes = Math.max(1, Math.round(originalWordCount / WORDS_PER_MINUTE));
  const summaryWordCount = summary.trim().split(/\s+/).filter(Boolean).length;
  const summaryMinutesRaw = summaryWordCount / WORDS_PER_MINUTE;
  const summaryMinutes = summaryMinutesRaw < 1 ? "<1" : Math.round(summaryMinutesRaw);
  const savedRaw = Math.max(0, originalMinutes - summaryMinutesRaw);
  const timeSavedLabel = savedRaw < 1 ? "<1 min" : `${Math.round(savedRaw)} min`;

  const processingTitle =
    inputMode === "text" ? "Analizando tu texto…" : inputMode === "url" ? "Leyendo la pagina…" : "Leyendo tu documento…";
  const processingSubtitle =
    inputMode === "text" ? "Procesando el texto pegado · esto toma unos segundos" : `${sourceName} · esto toma unos segundos`;

  return (
    <main>
      <ThemeToggle />
      <div className="card">
        {phase === "idle" && (
          <IdleView
            inputMode={inputMode}
            onInputModeChange={setInputMode}
            summaryLength={summaryLength}
            onSummaryLengthChange={setSummaryLength}
            outputLanguage={outputLanguage}
            onOutputLanguageChange={setOutputLanguage}
            pastedText={pastedText}
            onPastedTextChange={setPastedText}
            pastedUrl={pastedUrl}
            onPastedUrlChange={setPastedUrl}
            onSubmitFile={handleSubmitFile}
            onSubmitText={handleSubmitText}
            onSubmitUrl={handleSubmitUrl}
          />
        )}

        {phase === "processing" && (
          <ProcessingView title={processingTitle} subtitle={processingSubtitle} progress={progress} />
        )}

        {phase === "error" && <ErrorView message={errorMessage} onRetry={retryFromError} />}

        {phase === "done" && (
          <DoneView
            summary={summary}
            sourceName={sourceName}
            sourceMeta={sourceMeta}
            originalMinutes={originalMinutes}
            summaryMinutes={summaryMinutes}
            timeSavedLabel={timeSavedLabel}
            onReset={reset}
          />
        )}
        <ApiDocsLink />
      </div>
    </main>
  );
}

export default App;
