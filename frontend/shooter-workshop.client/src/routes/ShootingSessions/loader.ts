import {
  getWorkshopComponentDefinitions,
  getWorkshopComponents,
  getWorkshopFirearms,
  getWorkshopRecipes,
  getWorkshopShootingSessions,
} from "@morwalpizvideo/services";
import type { LoaderFunctionArgs } from "react-router";
import { deferRouteData } from "../shared";

export interface ShootingSessionsLoaderData {
  firearms: Awaited<ReturnType<typeof getWorkshopFirearms>>;
  recipes: Awaited<ReturnType<typeof getWorkshopRecipes>>;
  sessions: Awaited<ReturnType<typeof getWorkshopShootingSessions>>;
  definitions: Awaited<ReturnType<typeof getWorkshopComponentDefinitions>>;
  inventory: Awaited<ReturnType<typeof getWorkshopComponents>>;
}

export function loader(_args: LoaderFunctionArgs) {
  return deferRouteData(Promise.all([
    getWorkshopFirearms(),
    getWorkshopRecipes(),
    getWorkshopShootingSessions(),
    getWorkshopComponentDefinitions(),
    getWorkshopComponents(),
  ]).then(([firearms, recipes, sessions, definitions, inventory]): ShootingSessionsLoaderData => ({
    firearms,
    recipes,
    sessions,
    definitions,
    inventory,
  })));
}
