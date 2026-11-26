import { Navigate, type RouteObject } from "react-router";
import AppShell from "../layouts/AppShell";
import AccountLegalRedirect from "./AccountLegalRedirect";
import { Component as AccountLogin } from "./AccountLogin";
import { Component as AccountRegister } from "./AccountRegister";
import { Component as AccountProfile } from "./AccountProfile";
import { Component as Ammunition } from "./Ammunition";
import { Component as Dashboard } from "./Dashboard";
import { Component as FirearmDetail } from "./FirearmDetail";
import { Component as Firearms } from "./Firearms";
import { Component as History } from "./History";
import Inventory from "./Inventory/component";
import { Component as RecipeProduction, RecipeProductionError } from "./RecipeProduction";
import { Component as Reconciliation } from "./Reconciliation";
import { Component as ShootingSessions } from "./ShootingSessions";
import { loader as componentsLoader } from "./Components/loader";
import { action as ammunitionAction } from "./Ammunition/action";
import { ammunitionShouldRevalidate, loader as ammunitionLoader } from "./Ammunition/loader";
import { loader as firearmDetailLoader } from "./FirearmDetail/loader";
import { action as firearmDetailAction } from "./FirearmDetail/action";
import { loader as dashboardLoader } from "./Dashboard/loader";
import { loader as shootingSessionsLoader } from "./ShootingSessions/loader";
import { action as shootingSessionsAction } from "./ShootingSessions/action";
import { loader as historyLoader } from "./History/loader";
import { action as inventoryAction } from "./Inventory/action";
import { loader as inventoryLoader } from "./Inventory/loader";
import { firearmsShouldRevalidate, loader as firearmsLoader } from "./Firearms/loader";
import { action as firearmsAction } from "./Firearms/action";
import { loader as reconciliationLoader } from "./Reconciliation/loader";
import { action as reconciliationAction } from "./Reconciliation/action";
import { action as recipeProductionAction } from "./RecipeProduction/action";
import { loader as recipeProductionLoader } from "./RecipeProduction/loader";
import { FeedbackProvider } from "../components/feedback/FeedbackProvider";
import { DeferredRouteError } from "./shared";
import { action as offlineAction } from "./offlineAction";

export const routes: RouteObject[] = [
  { path: "/account/login", element: <AccountLogin /> },
  { path: "/account/register", element: <AccountRegister /> },
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
      { index: true, element: <Dashboard />, loader: dashboardLoader },
      { path: "firearms", element: <Firearms />, loader: firearmsLoader, action: firearmsAction, shouldRevalidate: firearmsShouldRevalidate },
      { path: "firearms/:id", element: <FirearmDetail />, loader: firearmDetailLoader, action: firearmDetailAction },
      { path: "components", loader: componentsLoader },
      { path: "maintenance", element: <Navigate to="/sessions/shooting?tab=maintenance" replace /> },
      {
        path: "ammunition",
        element: <Ammunition />,
        loader: ammunitionLoader,
        action: ammunitionAction,
        shouldRevalidate: ammunitionShouldRevalidate,
      },
      { path: "recipes", element: <Navigate to="/ammunition?tab=recipes" replace /> },
      {
        path: "recipes/:recipeId/produce",
        element: <RecipeProduction />,
        loader: recipeProductionLoader,
        action: recipeProductionAction,
        errorElement: <RecipeProductionError />,
      },
      { path: "production", element: <Navigate to="/ammunition" replace /> },
      { path: "sessions/shooting", element: <ShootingSessions />, loader: shootingSessionsLoader, action: shootingSessionsAction },
      { path: "history", element: <History />, loader: historyLoader },
      { path: "inventory", element: <Inventory />, loader: inventoryLoader, action: inventoryAction },
      { path: "integrity", element: <Reconciliation />, loader: reconciliationLoader, action: reconciliationAction },
      { path: "account/profile", element: <AccountProfile /> },
      { path: "*", element: <Navigate to="/" replace /> },
    ],
  },
];
