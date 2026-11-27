import { Navigate, type RouteObject } from "react-router";
import AppShell from "../layouts/AppShell";
import AccountLegalRedirect from "./AccountLegalRedirect";
import { FeedbackProvider } from "../components/feedback/FeedbackProvider";
import { DeferredRouteError } from "./shared";
import { action as offlineAction } from "./offlineAction";

export const routes: RouteObject[] = [
  ...["/account/forgot-password", "/account/reset-password", "/account/check-email", "/account/confirm-email"].map((path) => ({
    path,
    lazy: async () => ({ Component: (await import("./AccountMail")).default }),
  })),
  {
    path: "/account/login",
    lazy: async () => {
      const { Component } = await import("./AccountLogin");
      return { Component };
    },
  },
  {
    path: "/account/register",
    lazy: async () => {
      const { Component } = await import("./AccountRegister");
      return { Component };
    },
  },
  { path: "/privacy", element: <AccountLegalRedirect path="/privacy" /> },
  { path: "/terms", element: <AccountLegalRedirect path="/terms" /> },
  {
    path: "/cookie-policy",
    element: <AccountLegalRedirect path="/cookie-policy" />,
  },
  {
    path: "/",
    action: offlineAction,
    element: (
      <FeedbackProvider>
        <AppShell />
      </FeedbackProvider>
    ),
    errorElement: <DeferredRouteError />,
    children: [
      {
        index: true,
        lazy: async () => {
          const [component, loader] = await Promise.all([
            import("./Dashboard"),
            import("./Dashboard/loader"),
          ]);
          return { Component: component.Component, loader: loader.loader };
        },
      },
      {
        path: "firearms",
        lazy: async () => {
          const [component, loader, action] = await Promise.all([
            import("./Firearms"),
            import("./Firearms/loader"),
            import("./Firearms/action"),
          ]);
          return {
            Component: component.Component,
            loader: loader.loader,
            action: action.action,
            shouldRevalidate: loader.firearmsShouldRevalidate,
          };
        },
      },
      {
        path: "firearms/:id",
        lazy: async () => {
          const [component, loader, action] = await Promise.all([
            import("./FirearmDetail"),
            import("./FirearmDetail/loader"),
            import("./FirearmDetail/action"),
          ]);
          return { Component: component.Component, loader: loader.loader, action: action.action };
        },
      },
      {
        path: "components",
        lazy: async () => {
          const { loader } = await import("./Components/loader");
          return { loader };
        },
      },
      { path: "maintenance", element: <Navigate to="/sessions/shooting?tab=maintenance" replace /> },
      {
        path: "ammunition",
        lazy: async () => {
          const [component, loader, action] = await Promise.all([
            import("./Ammunition"),
            import("./Ammunition/loader"),
            import("./Ammunition/action"),
          ]);
          return {
            Component: component.Component,
            loader: loader.loader,
            action: action.action,
            shouldRevalidate: loader.ammunitionShouldRevalidate,
          };
        },
      },
      { path: "recipes", element: <Navigate to="/ammunition?tab=recipes" replace /> },
      {
        path: "recipes/:recipeId/produce",
        lazy: async () => {
          const [component, loader, action] = await Promise.all([
            import("./RecipeProduction"),
            import("./RecipeProduction/loader"),
            import("./RecipeProduction/action"),
          ]);
          return {
            Component: component.Component,
            loader: loader.loader,
            action: action.action,
            ErrorBoundary: component.RecipeProductionError,
          };
        },
      },
      { path: "production", element: <Navigate to="/ammunition" replace /> },
      {
        path: "sessions/shooting",
        lazy: async () => {
          const [component, loader, action] = await Promise.all([
            import("./ShootingSessions"),
            import("./ShootingSessions/loader"),
            import("./ShootingSessions/action"),
          ]);
          return { Component: component.Component, loader: loader.loader, action: action.action };
        },
      },
      {
        path: "history",
        lazy: async () => {
          const [component, loader] = await Promise.all([
            import("./History"),
            import("./History/loader"),
          ]);
          return { Component: component.Component, loader: loader.loader };
        },
      },
      {
        path: "inventory",
        lazy: async () => {
          const [component, loader, action] = await Promise.all([
            import("./Inventory/component"),
            import("./Inventory/loader"),
            import("./Inventory/action"),
          ]);
          return { Component: component.default, loader: loader.loader, action: action.action };
        },
      },
      {
        path: "integrity",
        lazy: async () => {
          const [component, loader, action] = await Promise.all([
            import("./Reconciliation"),
            import("./Reconciliation/loader"),
            import("./Reconciliation/action"),
          ]);
          return { Component: component.Component, loader: loader.loader, action: action.action };
        },
      },
      {
        path: "account/profile",
        lazy: async () => {
          const { Component } = await import("./AccountProfile");
          return { Component };
        },
      },
      { path: "*", element: <Navigate to="/" replace /> },
    ],
  },
];
