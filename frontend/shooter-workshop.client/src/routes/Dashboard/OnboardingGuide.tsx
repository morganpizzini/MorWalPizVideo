import { Link } from "react-router";
import {
  readWorkshopPreferences,
  writeWorkshopPreferences,
  type WorkshopOnboardingState,
} from "../../services/workshopPreferences";
import { useI18n } from "../../i18n";

interface OnboardingGuideProps {
  accountId: string;
  onDismiss: () => void;
}

interface OnboardingStep {
  title: string;
  description: string;
  path: string;
  action: string;
}

const stepCount = 4;

export default function OnboardingGuide({ accountId, onDismiss }: OnboardingGuideProps) {
  const { t } = useI18n();
  const preferences = readWorkshopPreferences(accountId);
  const currentStep = preferences.onboardingStep;

  const steps: OnboardingStep[] = [
    {
      title: t("Register your first firearm"),
      description: t("Start with the equipment you want to keep current."),
      path: "/firearms",
      action: t("Open firearms"),
    },
    {
      title: t("Add a component (optional)"),
      description: t("Keep a physical component and its history separate from its reusable definition."),
      path: "/components",
      action: t("Open components"),
    },
    {
      title: t("Create a recipe (optional)"),
      description: t("Save your own recipe record before recording production or tracked use."),
      path: "/recipes",
      action: t("Open recipes"),
    },
    {
      title: t("Record your first activity"),
      description: t("Record a shooting session or create ammunition from a recipe."),
      path: "/sessions/shooting",
      action: t("Record a shooting session"),
    },
  ];

  const saveOnboarding = (state: WorkshopOnboardingState, step: 0 | 1 | 2 | 3 | 4) => {
    const latest = readWorkshopPreferences(accountId);
    writeWorkshopPreferences(accountId, {
      ...latest,
      onboardingState: state,
      onboardingStep: step,
    });
  };

  const advanceTo = (step: number) => {
    const nextStep = Math.min(stepCount, Math.max(currentStep, step)) as 0 | 1 | 2 | 3 | 4;
    saveOnboarding("active", nextStep);
  };

  const finish = (state: "skipped" | "completed") => {
    saveOnboarding(state, state === "completed" ? stepCount : currentStep);
    onDismiss();
  };

  return (
    <section className="panel onboarding-panel" aria-labelledby="onboarding-title">
      <div className="onboarding-header">
        <div>
          <p className="eyebrow">{t("First-use guide")}</p>
          <h2 id="onboarding-title">{t("Set up your Workshop")}</h2>
          <p>{t("Create your first useful record, then keep exploring at your own pace.")}</p>
        </div>
        <span className="onboarding-progress" aria-label={t("{current} of {total} steps", { current: Math.min(currentStep + 1, stepCount), total: stepCount })}>
          {t("Step {current} of {total}", { current: Math.min(currentStep + 1, stepCount), total: stepCount })}
        </span>
      </div>
      <ol className="onboarding-steps">
        {steps.map((step, index) => {
          const completed = index < currentStep;
          const active = index === Math.min(currentStep, stepCount - 1);
          return (
            <li className={`onboarding-step${active ? " onboarding-step-current" : ""}${completed ? " onboarding-step-complete" : ""}`} key={step.path}>
              <span className="onboarding-step-number" aria-hidden="true">{completed ? "✓" : index + 1}</span>
              <div>
                <h3>{step.title}</h3>
                <p>{step.description}</p>
                <div className="onboarding-step-actions">
                  <Link className="button button-quiet" to={step.path} onClick={() => advanceTo(index + 1)}>
                    {step.action}
                  </Link>
                  {index === 3 ? (
                    <Link className="button button-quiet" to="/ammunition" onClick={() => advanceTo(index + 1)}>
                      {t("Record production")}
                    </Link>
                  ) : null}
                </div>
              </div>
            </li>
          );
        })}
      </ol>
      <p className="onboarding-note">
        {t("Tracked ammunition is resolved by the server. Component intervals are your records, not safety or load advice.")}
      </p>
      <div className="onboarding-actions">
        <button className="button button-quiet" type="button" onClick={() => finish("skipped")}>{t("Skip for now")}</button>
        <button className="button button-primary" type="button" onClick={() => finish("completed")}>{t("Finish setup")}</button>
      </div>
    </section>
  );
}
