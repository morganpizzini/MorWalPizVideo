import { getWorkshopAnalytics, getWorkshopDashboard, getWorkshopHistory } from "@morwalpizvideo/services";
import type { LoaderFunctionArgs } from "react-router";
import { deferRouteData } from "../shared";

export interface DashboardLoaderData {
  dashboard: Awaited<ReturnType<typeof getWorkshopDashboard>>;
  history: Awaited<ReturnType<typeof getWorkshopHistory>>;
  analytics: Awaited<ReturnType<typeof getWorkshopAnalytics>>;
  days: 30 | 90 | 365;
}

function daysFromRequest(request: Request) {
  const value = Number(new URL(request.url).searchParams.get("days"));
  return value === 90 || value === 365 ? value : 30;
}

export function loader({ request }: LoaderFunctionArgs) {
  const days = daysFromRequest(request);
  const to = new Date();
  const from = new Date(to.getTime() - days * 24 * 60 * 60 * 1000);
  return deferRouteData(Promise.all([
    getWorkshopDashboard(),
    getWorkshopHistory(),
    getWorkshopAnalytics(from.toISOString(), to.toISOString()),
  ]).then(([dashboard, history, analytics]): DashboardLoaderData => ({ dashboard, history, analytics, days })));
}
