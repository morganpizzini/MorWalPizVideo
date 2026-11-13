import { Navigate, type RouteObject } from "react-router";
import AppShell from "../layouts/AppShell";
import {
  AccountLogin,
  AccountProfile,
  Allocation,
  Ammunition,
  Components,
  Dashboard,
  FirearmDetail,
  Firearms,
  History,
  Maintenance,
  Production,
  Recipes,
  Reconciliation,
  ShootingSessions,
} from "./pages";

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
