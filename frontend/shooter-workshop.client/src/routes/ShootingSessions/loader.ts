import { getWorkshopFirearms, getWorkshopRecipes, getWorkshopShootingSessions } from "@morwalpizvideo/services";
import type { LoaderFunctionArgs } from "react-router";

export async function loader(_args: LoaderFunctionArgs) {
  const [firearms, recipes, sessions] = await Promise.all([
    getWorkshopFirearms(),
    getWorkshopRecipes(),
    getWorkshopShootingSessions(),
  ]);
  return { firearms, recipes, sessions };
}
