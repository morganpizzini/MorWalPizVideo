import { getWorkshopRecipe } from "@morwalpizvideo/services";
import type { LoaderFunctionArgs } from "react-router";

export async function loader({ params }: LoaderFunctionArgs) {
  if (!params.recipeId) throw new Response("Not found", { status: 404 });
  return getWorkshopRecipe(params.recipeId);
}

export type RecipeProductionLoaderData = Awaited<ReturnType<typeof loader>>;
