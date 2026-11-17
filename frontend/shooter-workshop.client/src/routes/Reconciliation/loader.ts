import { getWorkshopReconciliation } from "@morwalpizvideo/services";
import type { LoaderFunctionArgs } from "react-router";
import { deferRouteData } from "../shared";

export interface ReconciliationLoaderData {
  status: Awaited<ReturnType<typeof getWorkshopReconciliation>>;
}

export function loader(_args: LoaderFunctionArgs) {
  return deferRouteData(getWorkshopReconciliation().then((status): ReconciliationLoaderData => ({ status })));
}
