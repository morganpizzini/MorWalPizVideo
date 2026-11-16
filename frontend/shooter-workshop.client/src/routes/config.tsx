import { Navigate, type RouteObject } from "react-router";
import AppShell from "../layouts/AppShell";
import AccountLegalRedirect from "./AccountLegalRedirect";
import { Component as AccountLogin } from "./AccountLogin";
import { Component as AccountRegister } from "./AccountRegister";
import { Component as AccountProfile } from "./AccountProfile";
import { Component as Ammunition } from "./Ammunition";
import { Component as Components } from "./Components";
import { Component as Dashboard } from "./Dashboard";
import { Component as FirearmDetail } from "./FirearmDetail";
import { Component as Firearms } from "./Firearms";
import { Component as History } from "./History";
import { Component as Maintenance } from "./Maintenance";
import { Component as Recipes } from "./Recipes";
import { Component as RecipeProduction, RecipeProductionError } from "./RecipeProduction";
import { Component as Reconciliation } from "./Reconciliation";
import { Component as ShootingSessions } from "./ShootingSessions";
import { loader as componentsLoader } from "./Components/loader";
import { action as componentsAction } from "./Components/action";
import { action as ammunitionAction } from "./Ammunition/action";
import { loader as ammunitionLoader } from "./Ammunition/loader";
import { action as recipesAction } from "./Recipes/action";
import { loader as recipesLoader } from "./Recipes/loader";
import { loader as firearmDetailLoader } from "./FirearmDetail/loader";
import { action as firearmDetailAction } from "./FirearmDetail/action";
import { loader as dashboardLoader } from "./Dashboard/loader";
import { loader as shootingSessionsLoader } from "./ShootingSessions/loader";
import { action as shootingSessionsAction } from "./ShootingSessions/action";
import { loader as maintenanceLoader } from "./Maintenance/loader";
import { action as maintenanceAction } from "./Maintenance/action";
import { loader as historyLoader } from "./History/loader";
import { loader as firearmsLoader } from "./Firearms/loader";
import { action as firearmsAction } from "./Firearms/action";
import { loader as reconciliationLoader } from "./Reconciliation/loader";
import { action as reconciliationAction } from "./Reconciliation/action";
import { action as recipeProductionAction } from "./RecipeProduction/action";
import { loader as recipeProductionLoader } from "./RecipeProduction/loader";
import { FeedbackProvider } from "../components/feedback/FeedbackProvider";

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
    element: (
      <FeedbackProvider>
        <AppShell />
      </FeedbackProvider>
    ),
    children: [
      { index: true, element: <Dashboard />, loader: dashboardLoader },
      { path: "firearms", element: <Firearms />, loader: firearmsLoader, action: firearmsAction },
      { path: "firearms/:id", element: <FirearmDetail />, loader: firearmDetailLoader, action: firearmDetailAction },
      { path: "components", element: <Components />, loader: componentsLoader, action: componentsAction },
      { path: "maintenance", element: <Maintenance />, loader: maintenanceLoader, action: maintenanceAction },
      { path: "ammunition", element: <Ammunition />, loader: ammunitionLoader, action: ammunitionAction },
      { path: "recipes", element: <Recipes />, loader: recipesLoader, action: recipesAction },
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
      { path: "integrity", element: <Reconciliation />, loader: reconciliationLoader, action: reconciliationAction },
      { path: "account/profile", element: <AccountProfile /> },
      { path: "*", element: <Navigate to="/" replace /> },
    ],
  },
];
