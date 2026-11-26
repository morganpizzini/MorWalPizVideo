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
  UpdateWorkshopProductionRequest,
  CreateWorkshopInventoryProductRequest,
  UpdateWorkshopInventoryProductRequest,
  SetWorkshopInventoryProductArchivedRequest,
  DeleteWorkshopInventoryProductRequest,
  CreateWorkshopInventoryPurchaseRequest,
  AdjustWorkshopInventoryLotRequest,
  SetWorkshopInventoryLotStatusRequest,
  RecordWorkshopComponentPurchaseRequest,
  CorrectWorkshopComponentPurchaseRequest,
  CreateWorkshopRecipeRequest,
  UpdateWorkshopRecipeRequest,
  CreateWorkshopRecipeJournalEntryRequest,
  CreateWorkshopShootingRequest,
  CorrectWorkshopShootingRequest,
  WorkshopActivity,
  WorkshopAmmunitionBatch,
  WorkshopInventory,
  WorkshopInventoryProduct,
  WorkshopInventoryProductType,
  WorkshopInventoryLot,
  WorkshopInventoryPurchase,
  WorkshopComponentPurchase,
  WorkshopProductionMutationContract,
  WorkshopProductionRecord,
  WorkshopComponentDefinition,
  WorkshopFirearm,
  WorkshopOperationContract,
  WorkshopPhysicalComponent,
  WorkshopComponentInstallation,
  CreateAndInstallWorkshopComponentResponse,
  WorkshopRecipe,
  WorkshopRecipeJournalEntry,
  WorkshopReconciliationStatus,
  WorkshopMaintenanceEvent,
  WorkshopDashboard,
  WorkshopFirearmDetail,
  WorkshopProjectionState,
  WorkshopShootingSessionRecord,
  WorkshopAnalytics,
} from "@morwalpizvideo/models";
import frontendEndpoints from "./endpoints-frontend";
import { accountApiService, requireSuccessfulResponse } from "./apiTransport";

async function request<T>(operation: Promise<unknown>): Promise<T> {
  return requireSuccessfulResponse(await operation) as T;
}

const inventoryProductTypes: Record<string, WorkshopInventoryProductType> = {
  primer: "Primer",
  bullet: "Bullet",
  powder: "Powder",
  case: "Case",
  commercialammunition: "CommercialAmmunition",
};

function normalizeInventoryProduct(product: WorkshopInventoryProduct): WorkshopInventoryProduct {
  const type = inventoryProductTypes[product.type.toLowerCase()];
  if (!type) throw new Error(`Unsupported Workshop supply product type: ${product.type}`);
  return {
    ...product,
    type,
    revision: Number.isSafeInteger(product.revision) && product.revision >= 0 ? product.revision : 0,
  };
}

/** Every Workshop call intentionally uses accountApiService (cookie + CSRF). */
export const markWorkshopCapabilityGuideSeen = (
  version: number,
): Promise<{ effectiveVersion: number }> =>
  request(accountApiService.post(frontendEndpoints.WORKSHOP_CAPABILITY_GUIDE_SEEN, { version }));

export const getWorkshopFirearms = (): Promise<WorkshopFirearm[]> =>
  request(accountApiService.get(frontendEndpoints.WORKSHOP_FIREARMS));

export const getWorkshopFirearmDetail = (
  firearmId: string,
  options: { maintenancePage?: number; shootingPage?: number; installationPage?: number; pageSize?: number } = {},
): Promise<WorkshopFirearmDetail> => {
  const query = new URLSearchParams({
    maintenancePage: String(options.maintenancePage ?? 1),
    shootingPage: String(options.shootingPage ?? 1),
    installationPage: String(options.installationPage ?? 1),
    pageSize: String(options.pageSize ?? 15),
  });
  return request<Omit<WorkshopFirearmDetail, "projectionState"> & { projectionState: unknown }>(
    accountApiService.get(
      `${frontendEndpoints.WORKSHOP_FIREARM_DETAIL_READ.replace("{firearmId}", encodeURIComponent(firearmId))}?${query}`,
    ),
  ).then((detail) => ({ ...detail, projectionState: normalizeWorkshopProjectionState(detail.projectionState) }));
};

function normalizeWorkshopProjectionState(value: unknown): WorkshopProjectionState {
  if (value === 0 || value === "Current") return "Current";
  if (value === 1 || value === "Pending") return "Pending";
  if (value === 2 || value === "Inconsistent") return "Inconsistent";
  return "Unknown";
}

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

export const getWorkshopRecipe = (recipeId: string): Promise<WorkshopRecipe> =>
  request(accountApiService.get(
    frontendEndpoints.WORKSHOP_RECIPE_DETAIL.replace("{recipeId}", encodeURIComponent(recipeId)),
  ));

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

export const getWorkshopRecipeJournalEntries = (
  recipeId: string,
): Promise<WorkshopRecipeJournalEntry[]> =>
  request(accountApiService.get(
    frontendEndpoints.WORKSHOP_RECIPE_JOURNAL.replace("{recipeId}", encodeURIComponent(recipeId)),
  ));

export const createWorkshopRecipeJournalEntry = (
  recipeId: string,
  body: CreateWorkshopRecipeJournalEntryRequest,
): Promise<WorkshopRecipeJournalEntry> =>
  request(accountApiService.post(
    frontendEndpoints.WORKSHOP_RECIPE_JOURNAL.replace("{recipeId}", encodeURIComponent(recipeId)),
    body,
  ));

export const getWorkshopBatches = (): Promise<WorkshopAmmunitionBatch[]> =>
  request(accountApiService.get(`${frontendEndpoints.WORKSHOP_AMMUNITION}/batches`));

export const getWorkshopInventory = async (): Promise<WorkshopInventory> => {
  const inventory = await request<WorkshopInventory>(
    accountApiService.get(frontendEndpoints.WORKSHOP_INVENTORY),
  );
  return {
    ...inventory,
    products: inventory.products.map(normalizeInventoryProduct),
    supportsPackageQuantities: inventory.supportsPackageQuantities === true,
    supportsProductManagement: inventory.supportsProductManagement === true,
  };
};

export const createWorkshopInventoryProduct = async (
  body: CreateWorkshopInventoryProductRequest,
): Promise<WorkshopInventoryProduct> =>
  normalizeInventoryProduct(await request(accountApiService.post(
    frontendEndpoints.WORKSHOP_INVENTORY_PRODUCTS,
    body,
  )));

export const updateWorkshopInventoryProduct = async (
  productId: string,
  body: UpdateWorkshopInventoryProductRequest,
): Promise<WorkshopInventoryProduct> =>
  normalizeInventoryProduct(await request(accountApiService.put(
    frontendEndpoints.WORKSHOP_INVENTORY_PRODUCT_DETAIL.replace(
      "{productId}",
      encodeURIComponent(productId),
    ),
    body,
  )));

export const setWorkshopInventoryProductArchived = async (
  productId: string,
  body: SetWorkshopInventoryProductArchivedRequest,
): Promise<WorkshopInventoryProduct> =>
  normalizeInventoryProduct(await request(accountApiService.post(
    frontendEndpoints.WORKSHOP_INVENTORY_PRODUCT_ARCHIVE.replace(
      "{productId}",
      encodeURIComponent(productId),
    ),
    body,
  )));

export const deleteWorkshopInventoryProduct = (
  productId: string,
  body: DeleteWorkshopInventoryProductRequest,
): Promise<void> =>
  request(accountApiService.post(
    frontendEndpoints.WORKSHOP_INVENTORY_PRODUCT_DELETE.replace(
      "{productId}",
      encodeURIComponent(productId),
    ),
    body,
  ));

export const recordWorkshopInventoryPurchase = (
  body: CreateWorkshopInventoryPurchaseRequest,
): Promise<WorkshopInventoryPurchase> =>
  request(accountApiService.post(frontendEndpoints.WORKSHOP_INVENTORY_PURCHASES, body));

export const adjustWorkshopInventoryLot = (
  body: AdjustWorkshopInventoryLotRequest,
): Promise<WorkshopInventoryLot> =>
  request(accountApiService.post(frontendEndpoints.WORKSHOP_INVENTORY_ADJUSTMENTS, body));

export const setWorkshopInventoryLotStatus = (
  lotId: string,
  body: SetWorkshopInventoryLotStatusRequest,
): Promise<WorkshopInventoryLot> =>
  request(accountApiService.post(
    frontendEndpoints.WORKSHOP_INVENTORY_LOT_STATUS.replace("{lotId}", encodeURIComponent(lotId)),
    body,
  ));

export const recordWorkshopComponentPurchase = (
  componentId: string,
  body: RecordWorkshopComponentPurchaseRequest,
): Promise<WorkshopComponentPurchase> =>
  request(accountApiService.post(
    frontendEndpoints.WORKSHOP_COMPONENT_PURCHASE.replace("{componentId}", encodeURIComponent(componentId)),
    body,
  ));

export const correctWorkshopComponentPurchase = (
  componentId: string,
  body: CorrectWorkshopComponentPurchaseRequest,
): Promise<WorkshopComponentPurchase> =>
  request(accountApiService.put(
    frontendEndpoints.WORKSHOP_COMPONENT_PURCHASE.replace("{componentId}", encodeURIComponent(componentId)),
    body,
  ));

export const getWorkshopProductionRecords = (): Promise<WorkshopProductionRecord[]> =>
  request(accountApiService.get(frontendEndpoints.WORKSHOP_AMMUNITION_PRODUCTION));

export const recordWorkshopProduction = (
  body: CreateWorkshopProductionRequest,
): Promise<WorkshopProductionMutationContract> =>
  request(accountApiService.post(frontendEndpoints.WORKSHOP_AMMUNITION_PRODUCTION, body));

export const updateWorkshopProduction = (
  sessionId: string,
  body: UpdateWorkshopProductionRequest,
): Promise<WorkshopProductionMutationContract> =>
  request(accountApiService.put(
    frontendEndpoints.WORKSHOP_AMMUNITION_PRODUCTION_DETAIL.replace(
      "{sessionId}",
      encodeURIComponent(sessionId),
    ),
    body,
  ));

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

export const getWorkshopHistory = (
  options: { page?: number; pageSize?: number; type?: string } = {},
): Promise<WorkshopActivity[]> => {
  const query = new URLSearchParams();
  if (options.page !== undefined) query.set("page", String(options.page));
  if (options.pageSize !== undefined) query.set("pageSize", String(options.pageSize));
  if (options.type) query.set("type", options.type);
  const suffix = query.toString();
  return request(accountApiService.get(`${frontendEndpoints.WORKSHOP_HISTORY}${suffix ? `?${suffix}` : ""}`));
};

export const getWorkshopReconciliation = (): Promise<WorkshopReconciliationStatus> =>
  request(accountApiService.get(frontendEndpoints.WORKSHOP_RECONCILIATION));

export const rebuildWorkshopProjections = (): Promise<WorkshopReconciliationStatus> =>
  request(accountApiService.post(`${frontendEndpoints.WORKSHOP_RECONCILIATION}/rebuild`, {}));
