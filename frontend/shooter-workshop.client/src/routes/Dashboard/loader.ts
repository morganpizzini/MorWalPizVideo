import {
  getWorkshopAnalytics,
  getWorkshopDashboard,
  getWorkshopHistory,
  getWorkshopInventory,
} from "@morwalpizvideo/services";
import type { LoaderFunctionArgs } from "react-router";
import type { WorkshopInventory } from "@morwalpizvideo/models";

export interface DashboardLoaderData {
  dashboard: ReturnType<typeof getWorkshopDashboard>;
  history: ReturnType<typeof getWorkshopHistory>;
  analytics: ReturnType<typeof getWorkshopAnalytics>;
  inventory: Promise<WorkshopInventory | null>;
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
  return {
    dashboard: getWorkshopDashboard(),
    history: getWorkshopHistory(),
    analytics: getWorkshopAnalytics(from.toISOString(), to.toISOString()),
    inventory: typeof navigator !== "undefined" && !navigator.onLine
      ? Promise.resolve(null)
      : getWorkshopInventory(),
    days,
  } satisfies DashboardLoaderData;
}
