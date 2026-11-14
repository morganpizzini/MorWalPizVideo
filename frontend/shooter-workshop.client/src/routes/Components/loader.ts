import {
  getWorkshopComponentDefinitions,
  getWorkshopComponents,
  getWorkshopFirearms,
} from "@morwalpizvideo/services";

export async function loader() {
  const [definitions, inventory, firearms] = await Promise.all([
    getWorkshopComponentDefinitions(),
    getWorkshopComponents(),
    getWorkshopFirearms(),
  ]);
  return { definitions, inventory, firearms };
}

export type ComponentsLoaderData = Awaited<ReturnType<typeof loader>>;
