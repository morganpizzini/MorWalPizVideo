import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { createBrowserRouter } from "react-router";
import { RouterProvider } from "react-router/dom";
import { AuthProvider } from "./auth/AuthProvider";
import { FeedbackProvider } from "./components/feedback/FeedbackProvider";
import { routes } from "./routes/config";
import { I18nProvider } from "./i18n";
import "./services/installPrompt";
import "./main.scss";

if ("serviceWorker" in navigator) {
  void navigator.serviceWorker.register("/workshop-sw.js", { scope: "/" }).catch(error => {
    console.error("Unable to register the Workshop service worker", error);
  });
}

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <I18nProvider>
      <AuthProvider>
        <FeedbackProvider>
          <RouterProvider router={createBrowserRouter(routes)} />
        </FeedbackProvider>
      </AuthProvider>
    </I18nProvider>
  </StrictMode>,
);
