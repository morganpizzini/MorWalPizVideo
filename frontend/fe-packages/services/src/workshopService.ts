import type {
  CreateWorkshopAllocationRequest,
  CreateWorkshopComponentDefinitionRequest,
  CreateWorkshopFirearmRequest,
  UpdateWorkshopFirearmRequest,
  ArchiveWorkshopFirearmRequest,
  CreateWorkshopMaintenanceRequest,
  CreateWorkshopPhysicalComponentRequest,
  CreateWorkshopProductionRequest,
  CreateWorkshopRecipeRequest,
  CreateWorkshopShootingRequest,
  CorrectWorkshopShootingRequest,
  ReleaseWorkshopAllocationRequest,
  ReallocateWorkshopAmmunitionRequest,
  WorkshopActivity,
  WorkshopAmmunitionBatch,
  WorkshopComponentDefinition,
  WorkshopFirearm,
  WorkshopOperationContract,
  WorkshopPhysicalComponent,
  WorkshopRecipe,
  WorkshopReconciliationStatus,
  WorkshopMaintenanceEvent,
  WorkshopDashboard,
} from "@morwalpizvideo/models";
import frontendEndpoints from "./endpoints-frontend";
import { accountApiService, requireSuccessfulResponse } from "./apiTransport";

async function request<T>(operation: Promise<unknown>): Promise<T> {
  return requireSuccessfulResponse(await operation) as T;
}

/** Every Workshop call intentionally uses accountApiService (cookie + CSRF). */
export const getWorkshopFirearms = (): Promise<WorkshopFirearm[]> =>
  request(accountApiService.get(frontendEndpoints.WORKSHOP_FIREARMS));

export const createWorkshopFirearm = (
  body: CreateWorkshopFirearmRequest,
): Promise<WorkshopFirearm> =>
  request(accountApiService.post(frontendEndpoints.WORKSHOP_FIREARMS, body));

export const updateWorkshopFirearm = (
  firearmId: string,
  body: UpdateWorkshopFirearmRequest,
): Promise<WorkshopFirearm> =>
  request(accountApiService.put(
    frontendEndpoints.WORKSHOP_FIREARM_DETAIL.replace("{firearmId}", encodeURIComponent(firearmId)),
    body,
  ));

export const archiveWorkshopFirearm = (
  firearmId: string,
  body: ArchiveWorkshopFirearmRequest,
): Promise<WorkshopOperationContract> =>
  request(accountApiService.post(
    `${frontendEndpoints.WORKSHOP_FIREARM_DETAIL.replace("{firearmId}", encodeURIComponent(firearmId))}/archive`,
    body,
  ));

export const getWorkshopComponentDefinitions = (): Promise<WorkshopComponentDefinition[]> =>
  request(accountApiService.get(frontendEndpoints.WORKSHOP_COMPONENT_DEFINITIONS));

export const createWorkshopComponentDefinition = (
  body: CreateWorkshopComponentDefinitionRequest,
): Promise<WorkshopComponentDefinition> =>
  request(accountApiService.post(frontendEndpoints.WORKSHOP_COMPONENT_DEFINITIONS, body));

export const getWorkshopComponents = (): Promise<WorkshopPhysicalComponent[]> =>
  request(accountApiService.get(frontendEndpoints.WORKSHOP_COMPONENTS));

export const createWorkshopComponent = (
  body: CreateWorkshopPhysicalComponentRequest,
): Promise<WorkshopPhysicalComponent> =>
  request(accountApiService.post(frontendEndpoints.WORKSHOP_COMPONENTS, body));

export const recordWorkshopMaintenance = (
  body: CreateWorkshopMaintenanceRequest,
): Promise<WorkshopMaintenanceEvent> =>
  request(accountApiService.post(frontendEndpoints.WORKSHOP_MAINTENANCE, body));

export const getWorkshopRecipes = (): Promise<WorkshopRecipe[]> =>
  request(accountApiService.get(frontendEndpoints.WORKSHOP_RECIPES));

export const createWorkshopRecipe = (
  body: CreateWorkshopRecipeRequest,
): Promise<WorkshopRecipe> =>
  request(accountApiService.post(frontendEndpoints.WORKSHOP_RECIPES, body));

export const getWorkshopBatches = (): Promise<WorkshopAmmunitionBatch[]> =>
  request(accountApiService.get(`${frontendEndpoints.WORKSHOP_AMMUNITION}/batches`));

export const recordWorkshopProduction = (
  body: CreateWorkshopProductionRequest,
): Promise<WorkshopOperationContract> =>
  request(accountApiService.post(`${frontendEndpoints.WORKSHOP_AMMUNITION}/production`, body));

export const recordWorkshopShooting = (
  body: CreateWorkshopShootingRequest,
): Promise<WorkshopOperationContract> =>
  request(accountApiService.post(`${frontendEndpoints.WORKSHOP_SHOOTING}/shooting`, body));

export const allocateWorkshopAmmunition = (
  body: CreateWorkshopAllocationRequest,
): Promise<WorkshopOperationContract> =>
  request(accountApiService.post(`${frontendEndpoints.WORKSHOP_AMMUNITION}/allocations`, body));

export const releaseWorkshopAllocation = (
  allocationId: string,
  body: ReleaseWorkshopAllocationRequest,
): Promise<WorkshopOperationContract> =>
  request(accountApiService.post(
    `${frontendEndpoints.WORKSHOP_AMMUNITION}/allocations/${encodeURIComponent(allocationId)}/release`,
    body,
  ));

export const reallocateWorkshopAmmunition = (
  body: ReallocateWorkshopAmmunitionRequest,
): Promise<WorkshopOperationContract> =>
  request(accountApiService.post(`${frontendEndpoints.WORKSHOP_AMMUNITION}/allocations/reallocate`, body));

export const correctWorkshopShooting = (
  body: CorrectWorkshopShootingRequest,
): Promise<WorkshopOperationContract> =>
  request(accountApiService.post(frontendEndpoints.WORKSHOP_SHOOTING_CORRECT, body));

export const getWorkshopDashboard = (): Promise<WorkshopDashboard> =>
  request(accountApiService.get(frontendEndpoints.WORKSHOP_DASHBOARD));

export const getWorkshopHistory = (): Promise<WorkshopActivity[]> =>
  request(accountApiService.get(frontendEndpoints.WORKSHOP_HISTORY));

export const getWorkshopReconciliation = (): Promise<WorkshopReconciliationStatus> =>
  request(accountApiService.get(frontendEndpoints.WORKSHOP_RECONCILIATION));

export const rebuildWorkshopProjections = (): Promise<WorkshopReconciliationStatus> =>
  request(accountApiService.post(`${frontendEndpoints.WORKSHOP_RECONCILIATION}/rebuild`, {}));
