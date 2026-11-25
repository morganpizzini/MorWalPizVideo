import { useCallback, useMemo, useRef, useState, type ReactNode } from "react";
import { Toast as BootstrapToast, ToastContainer } from "react-bootstrap";
import { ToastContext, type Toast } from "./ToastContext.js";

export type ToastPosition =
  | "top-start"
  | "top-center"
  | "top-end"
  | "middle-start"
  | "middle-center"
  | "middle-end"
  | "bottom-start"
  | "bottom-center"
  | "bottom-end";

export interface ToastProviderProps {
  children: ReactNode;
  closeLabel?: string;
  className?: string;
  position?: ToastPosition;
}

export function ToastProvider({
  children,
  closeLabel = "Close",
  className = "p-3",
  position = "top-end",
}: ToastProviderProps) {
  const [toasts, setToasts] = useState<Toast[]>([]);
  const nextToastId = useRef(0);

  const show = useCallback(
    (
      title: string,
      message: string,
      options: Partial<Omit<Toast, "id" | "title" | "message">> = {},
    ) => {
      const newToast: Toast = {
        id: ++nextToastId.current,
        title,
        message,
        autohide: options.autohide !== undefined ? options.autohide : true,
        delay: options.autohide !== false && !options.delay ? 3000 : options.delay,
        ...options,
      };
      setToasts(previous => [...previous, newToast]);
    },
    [],
  );
  const contextValue = useMemo(() => ({ show }), [show]);
  const removeToast = (id: number) => {
    setToasts(previous => previous.filter(toast => toast.id !== id));
  };

  return (
    <ToastContext.Provider value={contextValue}>
      {children}
      <ToastContainer position={position} className={className} aria-live="polite" aria-atomic="true">
        {toasts.map(toast => (
          <BootstrapToast
            key={toast.id}
            bg={toast.variant}
            onClose={() => removeToast(toast.id)}
            show
            role="status"
            delay={toast.autohide ? toast.delay : undefined}
            autohide={toast.autohide}
          >
            <BootstrapToast.Header closeLabel={closeLabel}>
              <strong className="me-auto">{toast.title}</strong>
            </BootstrapToast.Header>
            <BootstrapToast.Body>{toast.message}</BootstrapToast.Body>
          </BootstrapToast>
        ))}
      </ToastContainer>
    </ToastContext.Provider>
  );
}
