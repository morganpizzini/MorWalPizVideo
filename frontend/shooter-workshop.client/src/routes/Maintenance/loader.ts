import { getWorkshopComponentDefinitions, getWorkshopComponents, getWorkshopFirearms } from "@morwalpizvideo/services";
import type { LoaderFunctionArgs } from "react-router";
import { loadOfflineAware } from "../../services/offlineData";

export async function loader(_args: LoaderFunctionArgs) {
  const [firearms, definitions, inventory] = await Promise.all([
    loadOfflineAware("firearms", getWorkshopFirearms),
    loadOfflineAware("definitions", getWorkshopComponentDefinitions),
    loadOfflineAware("components", getWorkshopComponents),
  ]);
  return { firearms, definitions, inventory };
}
