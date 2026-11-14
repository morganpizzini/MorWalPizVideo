import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { createBrowserRouter } from "react-router";
import { RouterProvider } from "react-router/dom";
import { AuthProvider } from "./auth/AuthProvider";
import { FeedbackProvider } from "./components/feedback/FeedbackProvider";
import { routes } from "./routes/config";
import { I18nProvider } from "./i18n";
import "./main.scss";

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
