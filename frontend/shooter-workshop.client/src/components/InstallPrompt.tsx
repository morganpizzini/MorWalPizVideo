import { useI18n } from "../i18n";
import { useInstallPrompt } from "../services/installPrompt";

export default function InstallPrompt() {
  const { t } = useI18n();
  const { canInstall, dismissed, installed, dismiss, install } = useInstallPrompt();

  if (!canInstall || dismissed || installed) return null;

  return (
    <aside className="install-prompt" aria-labelledby="install-prompt-title">
      <div>
        <h2 id="install-prompt-title">{t("Install MorWalPiz Workshop")}</h2>
        <p>{t("Open Workshop from your device like an app, while keeping your current sign-in flow.")}</p>
      </div>
      <div className="install-prompt__actions">
        <button className="button button-primary" onClick={() => void install()}>
          {t("Install")}
        </button>
        <button className="button button-quiet" onClick={dismiss}>
          {t("Not now")}
        </button>
      </div>
    </aside>
  );
}
