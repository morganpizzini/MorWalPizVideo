import { StrictMode } from "react";
import { createRoot, hydrateRoot } from "react-dom/client";
import { createBrowserRouter } from "react-router";
import { RouterProvider } from "react-router/dom";
import { AppearanceProvider, initializeAppearance } from "@morwalpiz/layout";
import "@morwalpiz/layout/theme";
import { routes } from "./routes";
import "./styles.css";

initializeAppearance();

const app = (
  <StrictMode>
    <AppearanceProvider>
      <RouterProvider router={createBrowserRouter(routes)} />
    </AppearanceProvider>
  </StrictMode>
);
const root = document.getElementById("root")!;
root.innerHTML.trim() ? hydrateRoot(root, app) : createRoot(root).render(app);
