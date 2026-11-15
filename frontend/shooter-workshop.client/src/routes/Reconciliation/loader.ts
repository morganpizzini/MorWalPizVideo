import { getWorkshopReconciliation } from "@morwalpizvideo/services";
import type { LoaderFunctionArgs } from "react-router";

export async function loader(_args: LoaderFunctionArgs) {
  return { status: await getWorkshopReconciliation() };
}

export type ReconciliationLoaderData = Awaited<ReturnType<typeof loader>>;
