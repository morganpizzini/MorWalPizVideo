import { getWorkshopRecipe } from "@morwalpizvideo/services";
import type { LoaderFunctionArgs } from "react-router";
import { loadOnlineInventory } from "../onlineInventory";
import type { OnlineInventoryAvailability } from "../onlineInventory";

export function loader({ params }: LoaderFunctionArgs) {
  if (!params.recipeId) throw new Response("Not found", { status: 404 });
  return {
    recipe: getWorkshopRecipe(params.recipeId),
    inventory: loadOnlineInventory(),
  } satisfies RecipeProductionLoaderResources;
}

export type RecipeProduction = Awaited<ReturnType<typeof getWorkshopRecipe>>;

export interface RecipeProductionLoaderResources {
  recipe: ReturnType<typeof getWorkshopRecipe>;
  inventory: Promise<OnlineInventoryAvailability>;
}
