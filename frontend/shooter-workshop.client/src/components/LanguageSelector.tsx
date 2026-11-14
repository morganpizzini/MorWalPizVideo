import { useI18n, supportedCultures, type SupportedCulture } from "../i18n";

export function LanguageSelector() {
  const { culture, setCulture, t } = useI18n();
  return (
    <label className="language-selector">
      <span>{t("Language")}</span>
      <select value={culture} onChange={(event) => setCulture(event.target.value as SupportedCulture)} aria-label={t("Language")}>
        {supportedCultures.map((option) => (
          <option value={option} key={option}>{option === "it-IT" ? t("Italian") : t("English")}</option>
        ))}
      </select>
    </label>
  );
}
