import { createPortal } from "react-dom";
import {
  useEffect,
  useId,
  useRef,
  useState,
  type MouseEvent,
  type ReactNode,
  type RefObject,
} from "react";

export interface DialogProps {
  open: boolean;
  title: ReactNode;
  description?: ReactNode;
  children: ReactNode;
  onClose: () => void;
  closeLabel: string;
  initialFocusRef?: RefObject<HTMLElement | null>;
  closeOnEscape?: boolean;
  closeOnBackdrop?: boolean;
  className?: string;
  backdropClassName?: string;
}

function getFocusable(dialog: HTMLElement): HTMLElement[] {
  return Array.from(dialog.querySelectorAll<HTMLElement>(
    'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), ' +
      'textarea:not([disabled]), [tabindex]:not([tabindex="-1"])',
  ));
}

export function Dialog({
  open,
  title,
  description,
  children,
  onClose,
  closeLabel,
  initialFocusRef,
  closeOnEscape = true,
  closeOnBackdrop = true,
  className,
  backdropClassName,
}: DialogProps) {
  const instanceId = useId();
  const [portalHost, setPortalHost] = useState<HTMLElement | null>(null);
  const dialogRef = useRef<HTMLDivElement>(null);
  const returnFocusRef = useRef<HTMLElement | null>(null);
  const onCloseRef = useRef(onClose);

  useEffect(() => {
    onCloseRef.current = onClose;
  }, [onClose]);

  useEffect(() => {
    if (typeof document === "undefined") return;
    const host = document.createElement("div");
    host.dataset.mwpDialogHost = instanceId;
    document.body.appendChild(host);
    setPortalHost(host);
    return () => host.remove();
  }, [instanceId]);

  useEffect(() => {
    if (!open || !portalHost) return;
    returnFocusRef.current = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    const background = Array.from(document.body.children).filter(
      (element): element is HTMLElement => element !== portalHost,
    );
    const previousState = background.map((element) => ({
      element,
      inert: element.inert,
      ariaHidden: element.getAttribute("aria-hidden"),
    }));
    background.forEach((element) => {
      element.inert = true;
      element.setAttribute("aria-hidden", "true");
    });

    const focusTarget = initialFocusRef?.current ?? getFocusable(dialogRef.current ?? portalHost)[0] ?? dialogRef.current;
    focusTarget?.focus();
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape" && closeOnEscape) {
        event.preventDefault();
        onCloseRef.current();
        return;
      }
      if (event.key !== "Tab" || !dialogRef.current) return;
      const focusable = getFocusable(dialogRef.current);
      if (focusable.length === 0) {
        event.preventDefault();
        dialogRef.current.focus();
        return;
      }
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
      previousState.forEach(({ element, inert, ariaHidden }) => {
        element.inert = inert;
        if (ariaHidden === null) element.removeAttribute("aria-hidden");
        else element.setAttribute("aria-hidden", ariaHidden);
      });
      returnFocusRef.current?.focus();
      returnFocusRef.current = null;
    };
  }, [closeOnEscape, initialFocusRef, open, portalHost]);

  if (!open || !portalHost) return null;
  const titleId = `${instanceId}-title`;
  const descriptionId = `${instanceId}-description`;
  const handleBackdropMouseDown = (event: MouseEvent<HTMLDivElement>) => {
    if (closeOnBackdrop && event.target === event.currentTarget) onClose();
  };

  return createPortal(
    <div
      className={`mwp-dialog-backdrop ${backdropClassName ?? ""}`}
      onMouseDown={handleBackdropMouseDown}
      data-mwp-dialog-backdrop
      style={{ maxHeight: "100vh", overflowY: "auto" }}
    >
      <div
        ref={dialogRef}
        className={`mwp-dialog ${className ?? ""}`}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={description ? descriptionId : undefined}
        tabIndex={-1}
      >
        <div className="mwp-dialog__header">
          <h2 id={titleId}>{title}</h2>
          <button type="button" className="button button-quiet" onClick={onClose} aria-label={closeLabel}>
            {closeLabel}
          </button>
        </div>
        {description ? <div id={descriptionId} className="mwp-dialog__description">{description}</div> : null}
        <div className="mwp-dialog__body">{children}</div>
      </div>
    </div>,
    portalHost,
  );
}

export default Dialog;
