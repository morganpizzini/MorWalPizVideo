import { getWorkshopAnalytics, getWorkshopDashboard, getWorkshopHistory } from "@morwalpizvideo/services";
import type { LoaderFunctionArgs } from "react-router";

function daysFromRequest(request: Request) {
  const value = Number(new URL(request.url).searchParams.get("days"));
  return value === 90 || value === 365 ? value : 30;
}

export async function loader({ request }: LoaderFunctionArgs) {
  const days = daysFromRequest(request);
  const to = new Date();
  const from = new Date(to.getTime() - days * 24 * 60 * 60 * 1000);
  const [dashboard, history, analytics] = await Promise.all([
    getWorkshopDashboard(),
    getWorkshopHistory(),
    getWorkshopAnalytics(from.toISOString(), to.toISOString()),
  ]);
  return { dashboard, history, analytics, days };
}
