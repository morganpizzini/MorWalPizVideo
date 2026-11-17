import { useEffect, useRef, useState, type MouseEvent } from "react";
import { Link } from "react-router";
import { useI18n } from "../i18n";

interface CapabilityStep {
  title: string;
  description: string;
  bullets: string[];
  actions?: { label: string; path: string }[];
}

interface CapabilityWizardProps {
  onClose: () => void;
  onSkip: () => void;
  onComplete: () => void;
  persistenceError?: string | null;
  onRetryPersistence?: () => void;
}

export default function CapabilityWizard({
  onClose,
  onSkip,
  onComplete,
  persistenceError,
  onRetryPersistence,
}: CapabilityWizardProps) {
  const { t } = useI18n();
  const [step, setStep] = useState(0);
  const dialogRef = useRef<HTMLElement>(null);
  const previouslyFocusedRef = useRef<HTMLElement | null>(null);
  const stepCount = 4;
  const steps: CapabilityStep[] = [
    {
      title: t("Welcome to Workshop"),
      description: t("Workshop is your private, account-owned record of equipment, ammunition and activity history."),
      bullets: [
        t("Take about one minute to see how the main areas fit together."),
        t("You can close this guide now and reopen it from the account controls."),
      ],
    },
    {
      title: t("Equipment and maintenance records"),
      description: t("Keep firearms, physical components, installations and factual maintenance entries in one place."),
      bullets: [
        t("Equipment history stays attributable to your account."),
        t("Maintenance entries are your records, not safety or replacement advice."),
      ],
      actions: [{ label: t("Open firearms"), path: "/firearms" }],
    },
    {
      title: t("Ammunition and recipes"),
      description: t("Create your own recipe records, production sessions, batches and stock history."),
      bullets: [
        t("The server resolves tracked ammunition and preserves operation history."),
        t("Workshop does not provide load recommendations or ballistic calculations."),
      ],
      actions: [{ label: t("Open ammunition"), path: "/ammunition" }],
    },
    {
      title: t("Record and review activity"),
      description: t("Capture shooting sessions, review correction-aware history and inspect your account summaries."),
      bullets: [
        t("Dashboard, History, Reports and Integrity help you review recorded activity."),
        t("Start with a real record whenever you are ready."),
      ],
      actions: [
        { label: t("Record a shooting session"), path: "/sessions/shooting" },
        { label: t("Open history"), path: "/history" },
      ],
    },
  ];
  const current = steps[step];

  useEffect(() => {
    previouslyFocusedRef.current = document.activeElement instanceof HTMLElement
      ? document.activeElement
      : null;
    dialogRef.current?.focus();
    const appContent = document.querySelector<HTMLElement>("main[data-feedback-app-content]");
    appContent?.setAttribute("inert", "");
    appContent?.setAttribute("aria-hidden", "true");

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        event.preventDefault();
        onClose();
        return;
      }
      if (event.key !== "Tab") return;
      const dialog = dialogRef.current;
      if (!dialog) return;
      const focusable = Array.from(dialog.querySelectorAll<HTMLElement>(
        "button:not([disabled]), a[href]",
      ));
      if (focusable.length === 0) return;
      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    };
    document.addEventListener("keydown", handleKeyDown);
    return () => {
      document.removeEventListener("keydown", handleKeyDown);
      appContent?.removeAttribute("inert");
      appContent?.removeAttribute("aria-hidden");
      previouslyFocusedRef.current?.focus();
    };
  }, [onClose]);

  const closeFromBackdrop = (event: MouseEvent<HTMLDivElement>) => {
    if (event.target === event.currentTarget) onClose();
  };

  return (
    <div className="capability-wizard-backdrop" role="presentation" onMouseDown={closeFromBackdrop}>
      <section
        ref={dialogRef}
        className="capability-wizard-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="capability-wizard-title"
        aria-describedby="capability-wizard-description"
        tabIndex={-1}
      >
        <header className="capability-wizard-header">
          <div>
            <p className="eyebrow">{t("Workshop guide")}</p>
            <h2 id="capability-wizard-title">{current.title}</h2>
          </div>
          <div className="capability-wizard-header-actions">
            <span aria-label={t("{current} of {total} steps", { current: step + 1, total: stepCount })}>
              {t("Step {current} of {total}", { current: step + 1, total: stepCount })}
            </span>
            <button type="button" className="button button-quiet" onClick={onClose}>{t("Close")}</button>
          </div>
        </header>
        <div className="capability-wizard-progress" aria-hidden="true">
          <span style={{ width: `${((step + 1) / stepCount) * 100}%` }} />
        </div>
        <div className="capability-wizard-body">
          <p id="capability-wizard-description">{current.description}</p>
          <ul>
            {current.bullets.map((bullet) => <li key={bullet}>{bullet}</li>)}
          </ul>
          {current.actions ? (
            <div className="capability-wizard-route-actions">
              {current.actions.map((action) => (
                <Link
                  className="button button-quiet"
                  key={action.path}
                  to={action.path}
                  onClick={onClose}
                >
                  {action.label}
                </Link>
              ))}
            </div>
          ) : null}
          {persistenceError ? (
            <div className="capability-wizard-error" role="alert">
              <p>{persistenceError}</p>
              {onRetryPersistence ? (
                <button type="button" className="button button-quiet" onClick={onRetryPersistence}>
                  {t("Retry")}
                </button>
              ) : null}
            </div>
          ) : null}
        </div>
        <footer className="capability-wizard-actions">
          <button type="button" className="button button-quiet" onClick={onSkip}>{t("Skip for now")}</button>
          <div>
            <button
              type="button"
              className="button button-quiet"
              onClick={() => setStep((currentStep) => Math.max(0, currentStep - 1))}
              disabled={step === 0}
            >
              {t("Back")}
            </button>
            {step < stepCount - 1 ? (
              <button type="button" className="button button-primary" onClick={() => setStep((currentStep) => currentStep + 1)}>
                {t("Next")}
              </button>
            ) : (
              <button type="button" className="button button-primary" onClick={onComplete}>
                {t("Finish guide")}
              </button>
            )}
          </div>
        </footer>
      </section>
    </div>
  );
}
