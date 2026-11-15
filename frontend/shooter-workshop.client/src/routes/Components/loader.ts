import {
  getWorkshopComponentDefinitions,
  getWorkshopComponentInstallations,
  getWorkshopComponents,
  getWorkshopFirearms,
} from "@morwalpizvideo/services";

export async function loader() {
  const [definitions, inventory, firearms, installations] = await Promise.all([
    getWorkshopComponentDefinitions(),
    getWorkshopComponents(),
    getWorkshopFirearms(),
    getWorkshopComponentInstallations(),
  ]);
  return { definitions, inventory, firearms, installations };
}

export type ComponentsLoaderData = Awaited<ReturnType<typeof loader>>;
