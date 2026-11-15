import { getWorkshopComponentDefinitions, getWorkshopComponents, getWorkshopFirearms } from "@morwalpizvideo/services";
import type { LoaderFunctionArgs } from "react-router";

export async function loader(_args: LoaderFunctionArgs) {
  const [firearms, definitions, inventory] = await Promise.all([
    getWorkshopFirearms(),
    getWorkshopComponentDefinitions(),
    getWorkshopComponents(),
  ]);
  return { firearms, definitions, inventory };
}
