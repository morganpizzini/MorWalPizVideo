import { getWorkshopBatches, getWorkshopRecipes } from "@morwalpizvideo/services";

export async function loader() {
  const [recipes, batches] = await Promise.all([
    getWorkshopRecipes(),
    getWorkshopBatches(),
  ]);
  return { recipes, batches };
}

export type AmmunitionLoaderData = Awaited<ReturnType<typeof loader>>;
