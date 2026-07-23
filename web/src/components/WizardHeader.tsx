import { InfoButton } from "./InfoButton";
import { ThemeToggle } from "./ThemeToggle";

export function WizardHeader() {
  return (
    <div className="card-header">
      <span className="card-logo">
        Te lo <span>resumo</span>
      </span>
      <div className="card-header-actions">
        <InfoButton />
        <ThemeToggle />
      </div>
    </div>
  );
}
