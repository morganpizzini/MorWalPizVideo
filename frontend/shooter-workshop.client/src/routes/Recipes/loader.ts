import { getWorkshopRecipes } from "@morwalpizvideo/services";

export async function loader() {
  return { recipes: await getWorkshopRecipes() };
}

export type RecipesLoaderData = Awaited<ReturnType<typeof loader>>;
