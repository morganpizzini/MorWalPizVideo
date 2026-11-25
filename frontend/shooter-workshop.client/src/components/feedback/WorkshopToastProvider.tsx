import { ToastProvider } from "@morwalpiz/layout/notifications";
import type { ReactNode } from "react";
import { useI18n } from "../../i18n";

export function WorkshopToastProvider({ children }: { children: ReactNode }) {
  const { t } = useI18n();
  return (
    <ToastProvider
      closeLabel={t("Dismiss notification")}
      className="p-3 workshop-toast-container"
    >
      {children}
    </ToastProvider>
  );
}
