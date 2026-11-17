import {
  getWorkshopComponentDefinitions,
  getWorkshopComponents,
  getWorkshopFirearms,
  getWorkshopRecipes,
  getWorkshopShootingSessions,
} from "@morwalpizvideo/services";
import type { LoaderFunctionArgs } from "react-router";

export async function loader(_args: LoaderFunctionArgs) {
  const [firearms, recipes, sessions, definitions, inventory] = await Promise.all([
    getWorkshopFirearms(),
    getWorkshopRecipes(),
    getWorkshopShootingSessions(),
    getWorkshopComponentDefinitions(),
    getWorkshopComponents(),
  ]);
  return { firearms, recipes, sessions, definitions, inventory };
}
