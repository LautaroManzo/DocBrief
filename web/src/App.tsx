import { useState } from "react";
import { SummaryForm } from "./components/SummaryForm";
import { SummaryResult } from "./components/SummaryResult";

function App() {
  const [summary, setSummary] = useState("");

  return (
    <main>
      <h1>DocBrief</h1>
      <p>Resumi PDFs o texto plano con IA.</p>

      <SummaryForm onResult={setSummary} />
      <SummaryResult summary={summary} />
    </main>
  );
}

export default App;
