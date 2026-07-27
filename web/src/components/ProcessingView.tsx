import { useEffect, useState } from "react";

const MESSAGES = ["Leyendo el contenido…", "Generando tu resumen…", "Casi listo…"];

export function ProcessingView() {
  const [messageIndex, setMessageIndex] = useState(0);

  useEffect(() => {
    const timer = setInterval(() => {
      setMessageIndex((i) => Math.min(i + 1, MESSAGES.length - 1));
    }, 2800);

    return () => clearInterval(timer);
  }, []);

  return (
    <div className="view">
      <div className="spinner">
        <div className="spinner-track" />
        <div className="spinner-arc" />
      </div>
      <h1 style={{ fontSize: 18 }}>{MESSAGES[messageIndex]}</h1>
    </div>
  );
}
