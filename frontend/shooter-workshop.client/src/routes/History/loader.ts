import { getWorkshopHistory } from "@morwalpizvideo/services";
import type { LoaderFunctionArgs } from "react-router";

export async function loader(_args: LoaderFunctionArgs) {
  return { items: await getWorkshopHistory() };
}
