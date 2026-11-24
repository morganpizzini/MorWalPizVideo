import { getWorkshopFirearmDetail } from "@morwalpizvideo/services";
import type { LoaderFunctionArgs } from "react-router";
import { deferRouteData } from "../shared";

export function loader({ params, request }: LoaderFunctionArgs) {
  const firearmId = params.id;
  if (!firearmId) throw new Response("Not found", { status: 404 });
  const url = new URL(request.url);
  return deferRouteData(getWorkshopFirearmDetail(firearmId, {
    maintenancePage: Number(url.searchParams.get("maintenancePage") ?? 1),
    shootingPage: Number(url.searchParams.get("shootingPage") ?? 1),
    installationPage: Number(url.searchParams.get("installationPage") ?? 1),
    pageSize: Number(url.searchParams.get("pageSize") ?? 15),
  }));
}

export type FirearmDetailLoaderData = Awaited<ReturnType<typeof getWorkshopFirearmDetail>>;
