import { getWorkshopFirearms } from "@morwalpizvideo/services";
import type { LoaderFunctionArgs } from "react-router";

export async function loader(_args: LoaderFunctionArgs) {
  return { firearms: await getWorkshopFirearms() };
}

export type FirearmsLoaderData = Awaited<ReturnType<typeof loader>>;
