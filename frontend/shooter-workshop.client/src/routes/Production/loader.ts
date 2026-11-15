import { getWorkshopRecipes } from "@morwalpizvideo/services";

export async function loader() {
  return { recipes: await getWorkshopRecipes() };
}

export type ProductionLoaderData = Awaited<ReturnType<typeof loader>>;
