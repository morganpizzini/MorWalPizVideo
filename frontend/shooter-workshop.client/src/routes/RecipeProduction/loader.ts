import { getWorkshopRecipe } from "@morwalpizvideo/services";
import type { LoaderFunctionArgs } from "react-router";
import { deferRouteData } from "../shared";

export function loader({ params }: LoaderFunctionArgs) {
  if (!params.recipeId) throw new Response("Not found", { status: 404 });
  return deferRouteData(getWorkshopRecipe(params.recipeId));
}

export type RecipeProductionLoaderData = Awaited<ReturnType<typeof getWorkshopRecipe>>;
