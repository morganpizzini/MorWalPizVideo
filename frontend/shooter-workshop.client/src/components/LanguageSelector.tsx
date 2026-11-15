import { useI18n, supportedCultures, type SupportedCulture } from "../i18n";

export function LanguageSelector() {
  const { culture, setCulture, t } = useI18n();
  return (
    <label className="language-selector icon-button" title={t("Language")}>
      <svg aria-hidden="true" viewBox="0 0 24 24" focusable="false"><circle cx="12" cy="12" r="8.5" /><path d="M3.8 12h16.4M12 3.5c2.2 2.3 3.3 5.1 3.3 8.5s-1.1 6.2-3.3 8.5c-2.2-2.3-3.3-5.1-3.3-8.5S9.8 5.8 12 3.5Z" /></svg>
      <select value={culture} onChange={(event) => setCulture(event.target.value as SupportedCulture)} aria-label={t("Language")}>
        {supportedCultures.map((option) => (
          <option value={option} key={option}>{option === "it-IT" ? t("Italian") : t("English")}</option>
        ))}
      </select>
    </label>
  );
}
