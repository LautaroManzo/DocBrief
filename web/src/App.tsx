import { useRef, useState } from "react";
import { ACCEPTED_EXTENSIONS, MAX_FILE_SIZE, MAX_TEXT_LENGTH } from "./constants";
import { DoneView } from "./components/DoneView";
import { ErrorView } from "./components/ErrorView";
import { ProcessingView } from "./components/ProcessingView";
import { StepContent } from "./components/StepContent";
import { StepProgressBar } from "./components/StepProgressBar";
import { StepSource } from "./components/StepSource";
import { WizardHeader } from "./components/WizardHeader";
import { summarizeFile, summarizeText, summarizeUrl } from "./services/api";
import type { InputMode, OutputLanguage, Phase, SummaryMode } from "./types";

function App() {
  const [phase, setPhase] = useState<Phase>("step1");
  const [inputMode, setInputMode] = useState<InputMode>("file");
  const [summaryMode, setSummaryMode] = useState<SummaryMode>("basico");
  const [outputLanguage, setOutputLanguage] = useState<OutputLanguage>("es");
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [pastedText, setPastedText] = useState("");
  const [pastedUrl, setPastedUrl] = useState("");
  const [progress, setProgress] = useState(0);
  const [errorMessage, setErrorMessage] = useState("");

  const [summary, setSummary] = useState("");
  const [sourceName, setSourceName] = useState("");
  const [sourceMeta, setSourceMeta] = useState("");

  const progressTimer = useRef<ReturnType<typeof setInterval> | null>(null);

  const step = phase === "step1" ? 1 : phase === "step2" ? 2 : 3;

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

  async function submitFile(file: File) {
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
      const result = await summarizeFile(file, { summaryMode, outputLanguage, includeConceptMap: summaryMode === "estudio" });
      stopProgressAnimation();
      setProgress(100);
      setSummary(result.summary);
      setPhase("done");
    } catch (error) {
      handleError(error);
    }
  }

  async function submitText() {
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
      const result = await summarizeText(pastedText, { summaryMode, outputLanguage, includeConceptMap: summaryMode === "estudio" });
      stopProgressAnimation();
      setProgress(100);
      setSummary(result.summary);
      setPhase("done");
    } catch (error) {
      handleError(error);
    }
  }

  async function submitUrl() {
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
      const result = await summarizeUrl(pastedUrl, { summaryMode, outputLanguage, includeConceptMap: summaryMode === "estudio" });
      stopProgressAnimation();
      setProgress(100);
      setSummary(result.summary);
      if (result.sourceTitle) setSourceName(result.sourceTitle);
      setPhase("done");
    } catch (error) {
      handleError(error);
    }
  }

  function goNext() {
    if (phase === "step1") {
      setPhase("step2");
      return;
    }

    if (phase === "step2") {
      if (inputMode === "file") {
        if (!selectedFile) {
          setErrorMessage("Elegí un archivo antes de continuar.");
          setPhase("error");
          return;
        }
        void submitFile(selectedFile);
      } else if (inputMode === "text") {
        void submitText();
      } else {
        void submitUrl();
      }
    }
  }

  function goBack() {
    if (phase === "step2") setPhase("step1");
  }

  function restart() {
    setPhase("step1");
    setSelectedFile(null);
    setPastedText("");
    setPastedUrl("");
    setSummary("");
    setProgress(0);
  }

  function retryFromError() {
    setPhase("step2");
  }

  return (
    <main>
      <div className="card">
        <WizardHeader />
        <StepProgressBar step={step} progress={progress} />

        <div className="card-body">
          {phase === "step1" && (
            <StepSource
              inputMode={inputMode}
              onInputModeChange={setInputMode}
              summaryMode={summaryMode}
              onSummaryModeChange={setSummaryMode}
              outputLanguage={outputLanguage}
              onOutputLanguageChange={setOutputLanguage}
            />
          )}

          {phase === "step2" && (
            <StepContent
              inputMode={inputMode}
              selectedFile={selectedFile}
              onSelectedFileChange={setSelectedFile}
              pastedText={pastedText}
              onPastedTextChange={setPastedText}
              pastedUrl={pastedUrl}
              onPastedUrlChange={setPastedUrl}
            />
          )}

          {phase === "processing" && <ProcessingView />}

          {phase === "error" && <ErrorView message={errorMessage} onRetry={retryFromError} />}

          {phase === "done" && (
            <DoneView summary={summary} sourceName={sourceName} sourceMeta={sourceMeta} onReset={restart} />
          )}

          {(phase === "step1" || phase === "step2") && (
            <div className="wizard-nav">
              {phase === "step2" && (
                <button className="btn-secondary" onClick={goBack}>
                  Atrás
                </button>
              )}
              <button className="btn-primary" style={{ marginLeft: "auto" }} onClick={goNext}>
                {phase === "step2" ? "Resumir" : "Siguiente"}
              </button>
            </div>
          )}
        </div>
      </div>
    </main>
  );
}

export default App;
