import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { createBrowserRouter } from "react-router";
import { RouterProvider } from "react-router/dom";
import { accountApiService } from "@morwalpizvideo/services";
import { AuthProvider } from "./auth/AuthProvider";
import { routes } from "./routes/config";
import { I18nProvider } from "./i18n";
import "./services/installPrompt";
import "./main.scss";

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
    <I18nProvider>
      <AuthProvider>
        <RouterProvider router={createBrowserRouter(routes)} />
      </AuthProvider>
    </I18nProvider>
  </StrictMode>,
);
