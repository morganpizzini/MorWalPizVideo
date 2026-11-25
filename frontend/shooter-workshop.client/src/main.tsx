import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { AppearanceProvider, initializeAppearance } from "@morwalpiz/layout";
import "@morwalpiz/layout/theme";
import { createBrowserRouter } from "react-router";
import { RouterProvider } from "react-router/dom";
import { accountApiService } from "@morwalpizvideo/services";
import { AuthProvider } from "./auth/AuthProvider";
import { WorkshopToastProvider } from "./components/feedback/WorkshopToastProvider";
import { routes } from "./routes/config";
import { I18nProvider } from "./i18n";
import "./services/installPrompt";
import "./main.scss";

initializeAppearance();

if ("serviceWorker" in navigator) {
  void navigator.serviceWorker.register("/workshop-sw.js", { scope: "/" }).catch(error => {
    console.error("Unable to register the Workshop service worker", error);
  });
}

accountApiService.setUnauthorizedHandler(() => {
  if (window.location.pathname === "/account/login") return;
  const returnUrl = `${window.location.pathname}${window.location.search}${window.location.hash}`;
  window.location.replace(`/account/login?returnUrl=${encodeURIComponent(returnUrl)}`);
});

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <AppearanceProvider>
      <I18nProvider>
        <WorkshopToastProvider>
          <AuthProvider>
            <RouterProvider router={createBrowserRouter(routes)} />
          </AuthProvider>
        </WorkshopToastProvider>
      </I18nProvider>
    </AppearanceProvider>
  </StrictMode>,
);
