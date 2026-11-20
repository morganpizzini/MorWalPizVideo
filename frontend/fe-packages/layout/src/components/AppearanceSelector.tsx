import { useId } from "react";
import {
  setAppearancePreference,
  useAppearance,
  type AppearancePreference,
} from "../appearance.js";

export interface AppearanceSelectorProps {
  label: string;
  systemLabel: string;
  lightLabel: string;
  darkLabel: string;
  persistenceErrorMessage: string;
}

export function AppearanceSelector({
  label,
  systemLabel,
  lightLabel,
  darkLabel,
  persistenceErrorMessage,
}: AppearanceSelectorProps) {
  const { preference, persistenceUnavailable } = useAppearance();
  const errorId = useId();

  return (
    <label className="mpv-appearance">
      <span className="mpv-appearance__label">{label}</span>
      <select
        className="mpv-appearance__select"
        value={preference}
        onChange={(event) => {
          const value = event.currentTarget.value;
          if (value === "system" || value === "light" || value === "dark") {
            setAppearancePreference(value satisfies AppearancePreference);
          }
        }}
        aria-describedby={persistenceUnavailable ? errorId : undefined}
      >
        <option value="system">{systemLabel}</option>
        <option value="light">{lightLabel}</option>
        <option value="dark">{darkLabel}</option>
      </select>
      {persistenceUnavailable && (
        <span className="mpv-appearance__error" id={errorId} role="status">
          {persistenceErrorMessage}
        </span>
      )}
    </label>
  );
}
