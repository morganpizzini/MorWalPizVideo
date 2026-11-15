/** TypeScript contracts matching ServerAPI's WorkshopController JSON surface. */

export type WorkshopTrackingMode = "Counter" | "Interval" | "Manual";
export type WorkshopProjectionState = "Current" | "Pending" | "Inconsistent";

export interface WorkshopFirearm {
  id: string;
  accountId: string;
  manufacturer: string;
  model: string;
  caliber: string;
  category?: string | null;
  nickname?: string | null;
  purchaseDate?: string | null;
  notes?: string | null;
  totalRounds: number;
  revision: number;
  archived: boolean;
}

export interface CreateWorkshopFirearmRequest {
  manufacturer: string;
  model: string;
  caliber: string;
  initialRounds?: number;
  category?: string | null;
  nickname?: string | null;
  purchaseDate?: string | null;
  notes?: string | null;
}

export interface UpdateWorkshopFirearmRequest extends CreateWorkshopFirearmRequest {
  revision: number;
}

export interface ArchiveWorkshopFirearmRequest {
  revision: number;
  idempotencyKey: string;
}

export interface WorkshopComponentDefinition {
  id: string;
  accountId: string;
  name: string;
  manufacturer?: string | null;
  model?: string | null;
  description?: string | null;
  defaultServiceInterval?: number | null;
  trackingMode: WorkshopTrackingMode;
  notes?: string | null;
  compatibilityTags: string[];
  archived: boolean;
  revision: number;
}

export interface CreateWorkshopComponentDefinitionRequest {
  name: string;
  description?: string | null;
  notes?: string | null;
  /** Legacy fields are accepted for rollout compatibility but ignored by new writes. */
  manufacturer?: string | null;
  model?: string | null;
  defaultServiceInterval?: number | null;
  trackingMode?: WorkshopTrackingMode;
  compatibilityTags?: string[];
}

export interface UpdateWorkshopComponentDefinitionRequest extends CreateWorkshopComponentDefinitionRequest {
  revision: number;
  idempotencyKey: string;
}

export interface ArchiveWorkshopComponentDefinitionRequest {
  revision: number;
  idempotencyKey: string;
}

export interface DeleteWorkshopComponentDefinitionRequest {
  revision: number;
  idempotencyKey: string;
}

export interface CreateAndInstallWorkshopComponentRequest
  extends CreateWorkshopPhysicalComponentRequest {
  firearmId?: string | null;
  installedAt: string;
  installationNotes?: string | null;
  idempotencyKey: string;
}

export interface InstallWorkshopComponentRequest {
  componentId: string;
  firearmId: string;
  installedAt: string;
  installationNotes?: string | null;
  idempotencyKey: string;
}

export interface RemoveWorkshopComponentRequest {
  removedAt: string;
  removalNotes?: string | null;
  idempotencyKey: string;
  revision: number;
}

export interface WorkshopComponentInstallation {
  id: string;
  accountId: string;
  componentId: string;
  firearmId: string;
  installedAt: string;
  removedAt?: string | null;
  installationNotes?: string | null;
  removalNotes?: string | null;
  revision: number;
}

export interface CreateAndInstallWorkshopComponentResponse {
  component: WorkshopPhysicalComponent;
  installation: WorkshopComponentInstallation;
}

export interface WorkshopPhysicalComponent {
  id: string;
  accountId: string;
  definitionId: string;
  identificationNote?: string | null;
  manufacturer?: string | null;
  model?: string | null;
  compatibilityTags: string[];
  totalRounds: number;
  serviceInterval?: number | null;
  trackingMode?: WorkshopTrackingMode;
  status: "Inventory" | "Installed" | "Maintenance" | "Retired";
  notes?: string | null;
  revision: number;
  archived: boolean;
}

export interface CreateWorkshopMaintenanceRequest {
  firearmId?: string | null;
  componentId?: string | null;
  eventType: "Inspection" | "Cleaning" | "ComponentMaintenance" | "Replacement" | "CounterAdjustment" | "General";
  date: string;
  roundCountAtEvent?: number | null;
  description: string;
  notes?: string | null;
  idempotencyKey: string;
}

export interface WorkshopMaintenanceEvent {
  id: string;
  accountId: string;
  firearmId?: string | null;
  componentId?: string | null;
  eventType: string;
  date: string;
  roundCountAtEvent?: number | null;
  description: string;
  notes?: string | null;
}

export interface CreateWorkshopPhysicalComponentRequest {
  definitionId: string;
  identificationNote?: string | null;
  manufacturer?: string | null;
  model?: string | null;
  serviceInterval?: number | null;
  trackingMode: WorkshopTrackingMode;
  compatibilityTags?: string[];
  notes?: string | null;
}

export interface UpdateWorkshopPhysicalComponentRequest extends CreateWorkshopPhysicalComponentRequest {
  revision: number;
  idempotencyKey: string;
}

export interface ArchiveWorkshopPhysicalComponentRequest {
  revision: number;
  idempotencyKey: string;
}

export interface DeleteWorkshopPhysicalComponentRequest {
  revision: number;
  idempotencyKey: string;
}

export interface WorkshopRecipe {
  id: string;
  accountId: string;
  name: string;
  caliber: string;
  oal?: string | null;
  bulletWeight?: number | null;
  bulletForm?: string | null;
  drawing?: string | null;
  primer?: string | null;
  powder?: string | null;
  powderWeight?: number | null;
  notes?: string | null;
  status: "Active" | "Archived";
  version: number;
  updatedAt: string;
}

export type CreateWorkshopRecipeRequest = Omit<
  WorkshopRecipe,
  "id" | "accountId" | "status" | "version" | "updatedAt"
>;

export interface UpdateWorkshopRecipeRequest
  extends CreateWorkshopRecipeRequest {
  version: number;
  idempotencyKey: string;
}

export interface WorkshopAmmunitionBatch {
  id: string;
  accountId: string;
  recipeId: string;
  recipeSnapshot: WorkshopRecipeSnapshot;
  productionSessionId: string;
  productionDate: string;
  producedQuantity: number;
  onHandQuantity: number;
  revision: number;
  notes?: string | null;
}

export interface WorkshopRecipeSnapshot {
  name: string;
  caliber: string;
  oal?: string | null;
  bulletWeight?: number | null;
  bulletForm?: string | null;
  drawing?: string | null;
  primer?: string | null;
  powder?: string | null;
  powderWeight?: number | null;
  notes?: string | null;
  version: number;
}

export interface WorkshopOperationContract {
  id: string;
  operationType: string;
  effectiveAt: string;
  recordedAt: string;
  contributionDigest: string;
}

export interface CreateWorkshopProductionRequest {
  recipeId: string;
  productionDate: string;
  quantityProduced: number;
  notes?: string | null;
  idempotencyKey: string;
}

export interface CreateWorkshopAllocationRequest {
  batchId: string;
  firearmId: string;
  quantity: number;
  notes?: string | null;
  idempotencyKey: string;
}

export interface ReleaseWorkshopAllocationRequest {
  quantity: number;
  revision: number;
  idempotencyKey: string;
}

export interface ReallocateWorkshopAmmunitionRequest {
  sourceAllocationId: string;
  destinationFirearmId: string;
  quantity: number;
  idempotencyKey: string;
}

export interface WorkshopConsumptionRequest {
  batchId?: string | null;
  quantity: number;
  allocationId?: string | null;
}

export interface WorkshopUsageSegmentRequest {
  componentId: string;
  installationId: string;
  quantity: number;
}

export interface CreateWorkshopShootingRequest {
  firearmId: string;
  date: string;
  totalRounds: number;
  notes?: string | null;
  consumptionRecords: WorkshopConsumptionRequest[];
  untrackedAmmunition: number;
  usageSegments: WorkshopUsageSegmentRequest[];
  idempotencyKey: string;
}

export interface CorrectWorkshopShootingRequest {
  originalOperationId: string;
  replacementShooting?: CreateWorkshopShootingRequest | null;
  idempotencyKey: string;
}

export interface WorkshopActivity {
  id: string;
  accountId: string;
  operationId: string;
  type: string;
  effectiveAt: string;
  sourceEntityIds: string[];
  summary?: string | null;
  entityLabel?: string | null;
}

export interface WorkshopReconciliationStatus {
  state: WorkshopProjectionState;
  mismatchCount: number;
  lastRunAt: string;
  mismatches: string[];
}

export interface WorkshopDashboard {
  firearmCount: number;
  componentCount: number;
  totalFirearmRounds: number;
  componentsAtOrAboveInterval: number;
  totalProducedRounds: number;
  availableRounds: number;
  allocatedRounds: number;
  recentActivityCount: number;
  projectionState: WorkshopProjectionState;
}
