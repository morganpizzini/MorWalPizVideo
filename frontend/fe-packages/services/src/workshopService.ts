import type {
  CreateWorkshopComponentDefinitionRequest,
  UpdateWorkshopComponentDefinitionRequest,
  ArchiveWorkshopComponentDefinitionRequest,
  DeleteWorkshopComponentDefinitionRequest,
  CreateAndInstallWorkshopComponentRequest,
  InstallWorkshopComponentRequest,
  RemoveWorkshopComponentRequest,
  CreateWorkshopFirearmRequest,
  UpdateWorkshopFirearmRequest,
  ArchiveWorkshopFirearmRequest,
  CreateWorkshopMaintenanceRequest,
  CreateWorkshopPhysicalComponentRequest,
  UpdateWorkshopPhysicalComponentRequest,
  ArchiveWorkshopPhysicalComponentRequest,
  DeleteWorkshopPhysicalComponentRequest,
  CreateWorkshopProductionRequest,
  CreateWorkshopRecipeRequest,
  UpdateWorkshopRecipeRequest,
  CreateWorkshopShootingRequest,
  CorrectWorkshopShootingRequest,
  WorkshopActivity,
  WorkshopAmmunitionBatch,
  WorkshopComponentDefinition,
  WorkshopFirearm,
  WorkshopOperationContract,
  WorkshopPhysicalComponent,
  WorkshopComponentInstallation,
  CreateAndInstallWorkshopComponentResponse,
  WorkshopRecipe,
  WorkshopReconciliationStatus,
  WorkshopMaintenanceEvent,
  WorkshopDashboard,
  WorkshopFirearmDetail,
  WorkshopShootingSessionRecord,
  WorkshopAnalytics,
} from "@morwalpizvideo/models";
import frontendEndpoints from "./endpoints-frontend";
import { accountApiService, requireSuccessfulResponse } from "./apiTransport";

async function request<T>(operation: Promise<unknown>): Promise<T> {
  return requireSuccessfulResponse(await operation) as T;
}

/** Every Workshop call intentionally uses accountApiService (cookie + CSRF). */
export const getWorkshopFirearms = (): Promise<WorkshopFirearm[]> =>
  request(accountApiService.get(frontendEndpoints.WORKSHOP_FIREARMS));

export const getWorkshopFirearmDetail = (
  firearmId: string,
  options: { maintenancePage?: number; shootingPage?: number; pageSize?: number } = {},
): Promise<WorkshopFirearmDetail> => {
  const query = new URLSearchParams({
    maintenancePage: String(options.maintenancePage ?? 1),
    shootingPage: String(options.shootingPage ?? 1),
    pageSize: String(options.pageSize ?? 15),
  });
  return request(accountApiService.get(
    `${frontendEndpoints.WORKSHOP_FIREARM_DETAIL_READ.replace("{firearmId}", encodeURIComponent(firearmId))}?${query}`,
  ));
};

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

export const updateWorkshopComponentDefinition = (
  definitionId: string,
  body: UpdateWorkshopComponentDefinitionRequest,
): Promise<WorkshopComponentDefinition> =>
  request(accountApiService.put(
    frontendEndpoints.WORKSHOP_COMPONENT_DEFINITION_DETAIL.replace("{definitionId}", encodeURIComponent(definitionId)), body,
  ));

export const archiveWorkshopComponentDefinition = (
  definitionId: string,
  body: ArchiveWorkshopComponentDefinitionRequest,
): Promise<WorkshopComponentDefinition> =>
  request(accountApiService.post(
    `${frontendEndpoints.WORKSHOP_COMPONENT_DEFINITION_DETAIL.replace("{definitionId}", encodeURIComponent(definitionId))}/archive`, body,
  ));

export const deleteWorkshopComponentDefinition = (
  definitionId: string,
  body: DeleteWorkshopComponentDefinitionRequest,
): Promise<void> =>
  request(accountApiService.Delete(
    frontendEndpoints.WORKSHOP_COMPONENT_DEFINITION_DETAIL.replace("{definitionId}", encodeURIComponent(definitionId)), body,
  ));

export const createAndInstallWorkshopComponent = (
  body: CreateAndInstallWorkshopComponentRequest,
): Promise<CreateAndInstallWorkshopComponentResponse> =>
  request(accountApiService.post(frontendEndpoints.WORKSHOP_COMPONENT_INSTALL, body));

export const getWorkshopComponents = (): Promise<WorkshopPhysicalComponent[]> =>
  request(accountApiService.get(frontendEndpoints.WORKSHOP_COMPONENTS));

export const getWorkshopComponentInstallations = (): Promise<WorkshopComponentInstallation[]> =>
  request(accountApiService.get(frontendEndpoints.WORKSHOP_COMPONENT_INSTALLATIONS));

export const createWorkshopComponent = (
  body: CreateWorkshopPhysicalComponentRequest,
): Promise<WorkshopPhysicalComponent> =>
  request(accountApiService.post(frontendEndpoints.WORKSHOP_COMPONENTS, body));

export const updateWorkshopComponent = (
  componentId: string,
  body: UpdateWorkshopPhysicalComponentRequest,
): Promise<WorkshopPhysicalComponent> =>
  request(accountApiService.put(
    frontendEndpoints.WORKSHOP_COMPONENT_DETAIL.replace("{componentId}", encodeURIComponent(componentId)), body,
  ));

export const archiveWorkshopComponent = (
  componentId: string,
  body: ArchiveWorkshopPhysicalComponentRequest,
): Promise<WorkshopPhysicalComponent> =>
  request(accountApiService.post(
    `${frontendEndpoints.WORKSHOP_COMPONENT_DETAIL.replace("{componentId}", encodeURIComponent(componentId))}/archive`, body,
  ));

export const deleteWorkshopComponent = (
  componentId: string,
  body: DeleteWorkshopPhysicalComponentRequest,
): Promise<void> =>
  request(accountApiService.Delete(
    frontendEndpoints.WORKSHOP_COMPONENT_DETAIL.replace("{componentId}", encodeURIComponent(componentId)), body,
  ));

export const installWorkshopComponent = (
  body: InstallWorkshopComponentRequest,
): Promise<WorkshopComponentInstallation> =>
  request(accountApiService.post(frontendEndpoints.WORKSHOP_COMPONENT_INSTALLATIONS, body));

export const removeWorkshopComponent = (
  installationId: string,
  body: RemoveWorkshopComponentRequest,
): Promise<WorkshopComponentInstallation> =>
  request(accountApiService.post(
    `${frontendEndpoints.WORKSHOP_COMPONENT_INSTALLATIONS}/${encodeURIComponent(installationId)}/remove`,
    body,
  ));

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

export const updateWorkshopRecipe = (
  recipeId: string,
  body: UpdateWorkshopRecipeRequest,
): Promise<WorkshopRecipe> =>
  request(accountApiService.put(
    `${frontendEndpoints.WORKSHOP_RECIPES}/${encodeURIComponent(recipeId)}`,
    body,
  ));

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

export const getWorkshopShootingSessions = (): Promise<WorkshopShootingSessionRecord[]> =>
  request(accountApiService.get(frontendEndpoints.WORKSHOP_SHOOTING_READ));

export const correctWorkshopShooting = (
  body: CorrectWorkshopShootingRequest,
): Promise<WorkshopOperationContract> =>
  request(accountApiService.post(frontendEndpoints.WORKSHOP_SHOOTING_CORRECT, body));

export const getWorkshopDashboard = (): Promise<WorkshopDashboard> =>
  request(accountApiService.get(frontendEndpoints.WORKSHOP_DASHBOARD));

export const getWorkshopAnalytics = (from: string, to: string): Promise<WorkshopAnalytics> => {
  const query = new URLSearchParams({ from, to });
  return request(accountApiService.get(`${frontendEndpoints.WORKSHOP_ANALYTICS}?${query}`));
};

export const getWorkshopHistory = (): Promise<WorkshopActivity[]> =>
  request(accountApiService.get(frontendEndpoints.WORKSHOP_HISTORY));

export const getWorkshopReconciliation = (): Promise<WorkshopReconciliationStatus> =>
  request(accountApiService.get(frontendEndpoints.WORKSHOP_RECONCILIATION));

export const rebuildWorkshopProjections = (): Promise<WorkshopReconciliationStatus> =>
  request(accountApiService.post(`${frontendEndpoints.WORKSHOP_RECONCILIATION}/rebuild`, {}));
