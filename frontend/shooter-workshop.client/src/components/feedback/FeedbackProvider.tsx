import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { ApiResponseError, submitFeedback } from "@morwalpizvideo/services";
import { useWorkshopAuth } from "../../auth/AuthProvider";
import { useI18n } from "../../i18n";

interface FeedbackContextValue {
  open: (componentKey?: string) => void;
}

const FeedbackContext = createContext<FeedbackContextValue | null>(null);

export function FeedbackProvider({ children }: { children: ReactNode }) {
  const { session } = useWorkshopAuth();
  const { t } = useI18n();
  const [open, setOpen] = useState(false);
  const [componentKey, setComponentKey] = useState<string>();
  const [description, setDescription] = useState("");
  const [context, setContext] = useState("");
  const [requestId, setRequestId] = useState("");
  const [pending, setPending] = useState(false);
  const [message, setMessage] = useState<string>();
  const [messageKind, setMessageKind] = useState<"error" | "success">("error");
  const descriptionRef = useRef<HTMLTextAreaElement>(null);
  const previouslyFocusedRef = useRef<HTMLElement | null>(null);
  const pendingRef = useRef(false);
  const successTimerRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);
  const appContentRef = useRef<HTMLDivElement>(null);
  const identityRef = useRef(session.authenticated);

  const launch = useCallback((key?: string) => {
    if (successTimerRef.current) clearTimeout(successTimerRef.current);
    if (!requestId) setComponentKey(key);
    if (!requestId) {
      setContext(`${window.location.origin}${window.location.pathname}`);
      setRequestId(crypto.randomUUID());
      setDescription("");
    }
    setMessage(undefined);
    setOpen(true);
  }, [requestId]);

  const close = () => {
    if (!pending) setOpen(false);
  };

  pendingRef.current = pending;

  useEffect(() => {
    if (!open) return;

    previouslyFocusedRef.current = document.activeElement instanceof HTMLElement
      ? document.activeElement
      : null;
    descriptionRef.current?.focus();
    if (appContentRef.current) {
      appContentRef.current.inert = true;
      appContentRef.current.setAttribute("aria-hidden", "true");
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        event.preventDefault();
        if (!pendingRef.current) setOpen(false);
        return;
      }

      if (event.key !== "Tab") return;
      const dialog = document.querySelector<HTMLElement>(".feedback-dialog");
      if (!dialog) return;
      const focusable = Array.from(dialog.querySelectorAll<HTMLElement>(
        'button:not([disabled]), textarea:not([disabled])',
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
      if (appContentRef.current) {
        appContentRef.current.inert = false;
        appContentRef.current.removeAttribute("aria-hidden");
      }
      previouslyFocusedRef.current?.focus();
      previouslyFocusedRef.current = null;
    };
  }, [open]);

  useEffect(() => {
    if (identityRef.current !== session.authenticated) {
      identityRef.current = session.authenticated;
      setDescription("");
      setContext("");
      setComponentKey(undefined);
      setRequestId("");
      setMessage(undefined);
      if (successTimerRef.current) clearTimeout(successTimerRef.current);
    }
  }, [session.authenticated]);

  useEffect(() => () => {
    if (successTimerRef.current) clearTimeout(successTimerRef.current);
  }, []);

  const send = async () => {
    if (!description.trim() || pending) return;
    setMessage(undefined);
    if (!requestId) {
      setMessageKind("error");
      setMessage(t("Unable to start a feedback report. Please try again."));
      return;
    }
    const payload = {
      requestId,
      description: description.trim(),
      context: {
        application: "Workshop",
        pagePath: context,
        componentKey,
        userAgent: navigator.userAgent,
        language: navigator.language,
        timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
        viewportWidth: window.innerWidth,
        viewportHeight: window.innerHeight,
        online: navigator.onLine,
      },
    };
    if (new TextEncoder().encode(JSON.stringify(payload)).byteLength > 16 * 1024) {
      setMessageKind("error");
      setMessage(t("Your report is too large. Shorten the description and try again."));
      return;
    }
    setPending(true);
    try {
      const receipt = await submitFeedback(payload);
      setMessageKind("success");
      setMessage(t(receipt.authenticated
        ? "Feedback sent. Thank you."
        : "Feedback sent anonymously. Thank you."));
      setDescription("");
      setRequestId("");
      successTimerRef.current = setTimeout(() => setOpen(false), 900);
    } catch (error) {
      setMessageKind("error");
      if (!navigator.onLine) setMessage(t("You appear to be offline. Reconnect and try again."));
      else if (error instanceof ApiResponseError && error.status === 429) setMessage(t("Too many reports. Please wait a moment and try again."));
      else if (error instanceof ApiResponseError && error.status === 403) setMessage(t("Your session expired. Refresh the page and try again."));
      else if (
        error instanceof ApiResponseError &&
        error.status !== undefined &&
        error.status >= 400 &&
        error.status < 500
      ) {
        setMessage(t("The report could not be accepted. Check the details and try again."));
      } else {
        setMessage(t("The feedback service is unavailable. Try again shortly."));
      }
    } finally {
      setPending(false);
    }
  };

  const value = useMemo(() => ({ open: launch }), [launch]);
  return (
    <FeedbackContext.Provider value={value}>
      <div ref={appContentRef} data-feedback-app-content>{children}</div>
      {session.authenticated ? (
        <button type="button" className="feedback-launcher" onClick={() => launch()}>
          {t("Report an issue")}
        </button>
      ) : null}
      {session.authenticated && open ? (
        <div className="feedback-backdrop" role="presentation" onMouseDown={event => {
          if (event.target === event.currentTarget) close();
        }}>
          <section className="feedback-dialog" role="dialog" aria-modal="true" aria-labelledby="feedback-title" aria-describedby="feedback-help">
            <div className="section-heading">
              <h2 id="feedback-title">{t("Report an issue")}</h2>
              <button type="button" className="button button-quiet" onClick={close} disabled={pending}>{t("Close")}</button>
            </div>
            <p id="feedback-help" className="feedback-help">{t("Describe what happened. The current page and basic browser context are attached automatically. Do not include passwords or private data.")}</p>
            <p className="feedback-context"><strong>{t("Page:")}</strong> {context}{componentKey ? ` · ${componentKey}` : ""}</p>
            <label htmlFor="feedback-description">{t("Description")}
              <textarea
                id="feedback-description"
                maxLength={4000}
                ref={descriptionRef}
                value={description}
                onChange={event => setDescription(event.target.value)}
                disabled={pending}
              />
            </label>
            {message ? <p className={`feedback-message feedback-message-${messageKind}`} role={messageKind === "error" ? "alert" : "status"}>{message}</p> : null}
            <div className="feedback-actions">
              <button type="button" className="button button-quiet" onClick={close} disabled={pending}>{t("Cancel")}</button>
              <button type="button" className="button button-primary" onClick={() => void send()} disabled={pending || !description.trim()}>
                {t(pending ? "Sending…" : "Send feedback")}
              </button>
            </div>
          </section>
        </div>
      ) : null}
    </FeedbackContext.Provider>
  );
}

export function useFeedback() {
  const context = useContext(FeedbackContext);
  if (!context) throw new Error("useFeedback must be used within FeedbackProvider");
  return context;
}
