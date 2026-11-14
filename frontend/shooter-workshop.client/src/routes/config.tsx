import { Navigate, type RouteObject } from "react-router";
import AppShell from "../layouts/AppShell";
import { Component as AccountLogin } from "./AccountLogin";
import { Component as AccountProfile } from "./AccountProfile";
import { Component as Allocation } from "./Allocation";
import { Component as Ammunition } from "./Ammunition";
import { Component as Components } from "./Components";
import { Component as Dashboard } from "./Dashboard";
import { Component as FirearmDetail } from "./FirearmDetail";
import { Component as Firearms } from "./Firearms";
import { Component as History } from "./History";
import { Component as Maintenance } from "./Maintenance";
import { Component as Production } from "./Production";
import { Component as Recipes } from "./Recipes";
import { Component as Reconciliation } from "./Reconciliation";
import { Component as ShootingSessions } from "./ShootingSessions";

export const routes: RouteObject[] = [
  { path: "/account/login", element: <AccountLogin /> },
  {
    path: "/",
    element: <AppShell />,
    children: [
      { index: true, element: <Dashboard /> },
      { path: "firearms", element: <Firearms /> },
      { path: "firearms/:id", element: <FirearmDetail /> },
      { path: "components", element: <Components /> },
      { path: "maintenance", element: <Maintenance /> },
      { path: "ammunition", element: <Ammunition /> },
      { path: "ammunition/allocations", element: <Allocation /> },
      { path: "recipes", element: <Recipes /> },
      { path: "production", element: <Production /> },
      { path: "sessions/shooting", element: <ShootingSessions /> },
      { path: "history", element: <History /> },
      { path: "integrity", element: <Reconciliation /> },
      { path: "account/profile", element: <AccountProfile /> },
      { path: "*", element: <Navigate to="/" replace /> },
    ],
  },
];
