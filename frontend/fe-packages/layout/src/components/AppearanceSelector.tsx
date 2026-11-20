import { Check, ChevronDown, Monitor } from "lucide-react";
import { useEffect, useId, useRef, useState, type KeyboardEvent } from "react";
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
  const id = useId();
  const [isOpen, setIsOpen] = useState(false);
  const rootRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const labels: Record<AppearancePreference, string> = {
    system: systemLabel,
    light: lightLabel,
    dark: darkLabel,
  };

  useEffect(() => {
    if (!isOpen) return;

    const root = rootRef.current;
    root
      ?.querySelector<HTMLButtonElement>(
        '[role="menuitemradio"][aria-checked="true"]',
      )
      ?.focus();

    function closeOnOutsidePointer(event: PointerEvent) {
      if (!root?.contains(event.target as Node)) setIsOpen(false);
    }

    document.addEventListener("pointerdown", closeOnOutsidePointer);
    return () =>
      document.removeEventListener("pointerdown", closeOnOutsidePointer);
  }, [isOpen]);

  function closeMenu(returnFocus: boolean) {
    setIsOpen(false);
    if (returnFocus) triggerRef.current?.focus();
  }

  function handleMenuKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === "Escape") {
      event.preventDefault();
      closeMenu(true);
      return;
    }

    const items = Array.from(
      event.currentTarget.querySelectorAll<HTMLButtonElement>(
        '[role="menuitemradio"]',
      ),
    );
    const currentIndex = items.indexOf(
      document.activeElement as HTMLButtonElement,
    );
    let nextIndex = currentIndex;

    if (event.key === "ArrowDown")
      nextIndex = (currentIndex + 1) % items.length;
    else if (event.key === "ArrowUp") {
      nextIndex = (currentIndex - 1 + items.length) % items.length;
    } else if (event.key === "Home") nextIndex = 0;
    else if (event.key === "End") nextIndex = items.length - 1;
    else return;

    event.preventDefault();
    items[nextIndex]?.focus();
  }

  return (
    <div className="mpv-appearance" ref={rootRef}>
      <button
        ref={triggerRef}
        className="mpv-appearance__trigger"
        type="button"
        aria-label={`${label}: ${labels[preference]}`}
        aria-haspopup="menu"
        aria-expanded={isOpen}
        aria-controls={`${id}-menu`}
        aria-describedby={persistenceUnavailable ? `${id}-error` : undefined}
        title={label}
        onClick={() => setIsOpen((open) => !open)}
        onKeyDown={(event) => {
          if (
            !isOpen &&
            (event.key === "ArrowDown" || event.key === "ArrowUp")
          ) {
            event.preventDefault();
            setIsOpen(true);
          }
        }}
      >
        <Monitor aria-hidden="true" focusable="false" />
      </button>
      <div
        className="mpv-appearance__menu"
        id={`${id}-menu`}
        role="menu"
        aria-label={label}
        hidden={!isOpen}
        onKeyDown={handleMenuKeyDown}
        onBlur={(event) => {
          if (
            !event.currentTarget.contains(event.relatedTarget as Node | null)
          ) {
            setIsOpen(false);
          }
        }}
      >
        {(
          [
            ["system", systemLabel],
            ["light", lightLabel],
            ["dark", darkLabel],
          ] as const
        ).map(([value, optionLabel]) => (
          <button
            key={value}
            className="mpv-appearance__option"
            data-appearance-preference={value}
            type="button"
            role="menuitemradio"
            aria-checked={preference === value}
            tabIndex={preference === value ? 0 : -1}
            onClick={() => {
              setAppearancePreference(value);
              closeMenu(true);
            }}
          >
            <span>{optionLabel}</span>
            <Check
              className="mpv-appearance__check"
              aria-hidden="true"
              focusable="false"
            />
          </button>
        ))}
      </div>
      {persistenceUnavailable && (
        <span
          className="mpv-appearance__error"
          id={`${id}-error`}
          role="status"
        >
          {persistenceErrorMessage}
        </span>
      )}
    </div>
  );
}
