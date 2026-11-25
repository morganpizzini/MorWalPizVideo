import {
  getWorkshopComponentDefinitions,
  getWorkshopComponents,
  getWorkshopFirearms,
  getWorkshopBatches,
  getWorkshopRecipes,
  getWorkshopShootingSessions,
} from "@morwalpizvideo/services";
import type { LoaderFunctionArgs } from "react-router";
import { deferRouteData } from "../shared";
import { loadOfflineAware } from "../../services/offlineData";

export interface ShootingSessionsLoaderData {
  firearms: Awaited<ReturnType<typeof getWorkshopFirearms>>;
  recipes: Awaited<ReturnType<typeof getWorkshopRecipes>>;
  sessions: Awaited<ReturnType<typeof getWorkshopShootingSessions>>;
  definitions: Awaited<ReturnType<typeof getWorkshopComponentDefinitions>>;
  inventory: Awaited<ReturnType<typeof getWorkshopComponents>>;
  batches: Awaited<ReturnType<typeof getWorkshopBatches>>;
}

export function loader(_args: LoaderFunctionArgs) {
  return deferRouteData(Promise.all([
    loadOfflineAware("firearms", getWorkshopFirearms),
    loadOfflineAware("recipes", getWorkshopRecipes),
    loadOfflineAware("sessions", getWorkshopShootingSessions),
    loadOfflineAware("definitions", getWorkshopComponentDefinitions),
    loadOfflineAware("components", getWorkshopComponents),
    loadOfflineAware("batches", getWorkshopBatches),
  ]).then(([firearms, recipes, sessions, definitions, inventory, batches]): ShootingSessionsLoaderData => ({
    firearms,
    recipes,
    sessions,
    definitions,
    inventory,
    batches,
  })));
}
