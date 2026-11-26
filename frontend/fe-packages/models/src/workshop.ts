/** TypeScript contracts matching ServerAPI's WorkshopController JSON surface. */

export type WorkshopTrackingMode = "Counter" | "Interval" | "Manual";
export type WorkshopMaintenanceEventType = "Inspection" | "Cleaning" | "ComponentMaintenance" | "Replacement" | "CounterAdjustment" | "General" | "Malfunction";
export type WorkshopProjectionState = "Current" | "Pending" | "Inconsistent" | "Unknown";

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
  idempotencyKey?: string;
}

export interface UpdateWorkshopFirearmRequest extends CreateWorkshopFirearmRequest {
  revision: number;
  idempotencyKey: string;
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
  idempotencyKey?: string;
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
  eventType: WorkshopMaintenanceEventType;
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
  initialRounds?: number;
  compatibilityTags?: string[];
  notes?: string | null;
  idempotencyKey?: string;
}

export interface UpdateWorkshopPhysicalComponentRequest extends CreateWorkshopPhysicalComponentRequest {
  totalRounds?: number | null;
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
  primer?: string | null;
  primerProductId?: string | null;
  powder?: string | null;
  powderProductId?: string | null;
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

export type WorkshopRecipeJournalTimeOfDay =
  | "Morning"
  | "Noon"
  | "Afternoon"
  | "Evening";

export interface WorkshopRecipeJournalEntry {
  id: string;
  recipeId: string;
  entryDate: string;
  timeOfDay?: WorkshopRecipeJournalTimeOfDay | null;
  weather?: string | null;
  temperature?: string | null;
  temperatureUnit?: "C" | "F" | null;
  powerFactor?: string | null;
  notes?: string | null;
  createdAt: string;
}

export interface CreateWorkshopRecipeJournalEntryRequest {
  entryDate: string;
  timeOfDay?: WorkshopRecipeJournalTimeOfDay | null;
  weather?: string | null;
  temperature?: string | null;
  temperatureUnit?: "C" | "F" | null;
  powerFactor?: string | null;
  notes?: string | null;
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
  primer?: string | null;
  primerProductId?: string | null;
  powder?: string | null;
  powderProductId?: string | null;
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
  idempotent?: boolean;
}

export interface WorkshopProductionMutationContract extends WorkshopOperationContract {
  session: WorkshopProductionSession;
  batch: WorkshopAmmunitionBatch;
}

export interface WorkshopProductionCorrectionContract {
  operationId: string;
  recordedAt: string;
  correction: {
    expectedRevision: number;
    previousProductionDate: string;
    previousQuantity: number;
    previousBulletProductId: string;
    previousNotes?: string | null;
    previousPrimerLotAllocations: WorkshopProductionLotAllocation[];
    previousBulletLotAllocations: WorkshopProductionLotAllocation[];
    productionDate: string;
    quantity: number;
    bulletProductId: string;
    notes?: string | null;
    primerLotAllocations: WorkshopProductionLotAllocation[];
    bulletLotAllocations: WorkshopProductionLotAllocation[];
  };
}

export interface WorkshopProductionRecord {
  session: WorkshopProductionSession;
  corrections: WorkshopProductionCorrectionContract[];
  projectionPending: boolean;
}

export interface CreateWorkshopProductionRequest {
  recipeId: string;
  productionDate: string;
  quantityProduced: number;
  bulletProductId: string;
  primerLotAllocations: WorkshopProductionLotAllocation[];
  bulletLotAllocations: WorkshopProductionLotAllocation[];
  notes?: string | null;
  idempotencyKey: string;
}

export interface WorkshopProductionLotAllocation {
  lotId: string;
  quantity: number;
  lotRevision?: number;
}

export interface UpdateWorkshopProductionRequest extends CreateWorkshopProductionRequest {
  expectedRevision: number;
}

export interface WorkshopProductionSession {
  id: string;
  accountId: string;
  recipeId: string;
  recipeSnapshot: WorkshopRecipeSnapshot;
  productionDate: string;
  quantityProduced: number;
  batchId: string;
  bulletProductId: string;
  primerLotAllocations: WorkshopProductionLotAllocation[];
  bulletLotAllocations: WorkshopProductionLotAllocation[];
  revision: number;
  notes?: string | null;
}

export type WorkshopInventoryProductType =
  | "Primer"
  | "Bullet"
  | "Powder"
  | "Case"
  | "CommercialAmmunition";

export type WorkshopInventoryLotStatus = "Available" | "Empty";

export interface WorkshopInventoryProduct {
  id: string;
  type: WorkshopInventoryProductType;
  brand?: string | null;
  name: string;
  archived: boolean;
}

export interface CreateWorkshopInventoryProductRequest {
  type: WorkshopInventoryProductType;
  brand?: string | null;
  name: string;
  idempotencyKey: string;
}

export interface WorkshopInventoryLot {
  id: string;
  productId: string;
  purchaseDate: string;
  purchasedQuantity: number;
  availableQuantity: number;
  totalPrice: number;
  currency: string;
  isEmpty: boolean;
  itemCount?: number | null;
  revision: number;
}

export interface CreateWorkshopInventoryPurchaseRequest {
  productId: string;
  purchaseDate: string;
  quantity: number;
  totalPrice: number;
  currency: string;
  idempotencyKey: string;
}

export interface WorkshopInventoryPurchase {
  id: string;
  productId: string;
  purchaseDate: string;
  quantity: number;
  totalPrice: number;
  currency: string;
  lots: WorkshopInventoryLot[];
}

export interface AdjustWorkshopInventoryLotRequest {
  lotId: string;
  physicalCount: number;
  reason: string;
  expectedRevision: number;
  idempotencyKey: string;
}

export interface SetWorkshopInventoryLotStatusRequest {
  isEmpty: boolean;
  expectedRevision: number;
  idempotencyKey: string;
}

export interface WorkshopCurrencyTotal {
  currency: string;
  total: number;
}

export interface RecordWorkshopComponentPurchaseRequest {
  purchaseDate: string;
  totalPrice: number;
  currency: string;
  idempotencyKey: string;
}

export interface CorrectWorkshopComponentPurchaseRequest
  extends RecordWorkshopComponentPurchaseRequest {
  expectedRevision: number;
  reason: string;
}

export interface WorkshopComponentPurchaseRevision {
  revision: number;
  purchaseDate: string;
  totalPrice: number;
  currency: string;
  reason?: string | null;
  recordedAt: string;
}

export interface WorkshopComponentPurchase {
  id: string;
  componentId: string;
  purchaseDate: string;
  totalPrice: number;
  currency: string;
  revision: number;
  history: WorkshopComponentPurchaseRevision[];
}

export interface WorkshopInventory {
  products: WorkshopInventoryProduct[];
  lots: WorkshopInventoryLot[];
  componentPurchases: WorkshopComponentPurchase[];
  spending: WorkshopCurrencyTotal[];
  partialConsumptionCosts: WorkshopCurrencyTotal[];
  missingComponentPrices: string[];
}

export interface WorkshopInventorySummary {
  productCount: number;
  availableLotCount: number;
  primerUnitsAvailable: number;
  bulletUnitsAvailable: number;
}

export interface WorkshopConsumptionRequest {
  recipeId: string;
  quantity: number;
}

export interface WorkshopResolvedConsumption {
  batchId?: string | null;
  recipeId?: string | null;
  quantity: number;
  allocationId?: string | null;
}

export interface WorkshopShootingSessionRecord {
  session: {
    id: string;
    accountId: string;
    operationId?: string | null;
    firearmId: string;
    date: string;
    totalRounds: number;
    notes?: string | null;
    consumptionRecords: WorkshopResolvedConsumption[];
    usageSegments: WorkshopUsageSegmentRequest[];
    untrackedAmmunition: number;
    correctionOfId?: string | null;
  };
  superseded: boolean;
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
  ammunitionRecords: WorkshopConsumptionRequest[];
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
  recordedAt?: string | null;
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
  recentActivityCount: number;
  projectionState: WorkshopProjectionState;
}

export interface WorkshopAnalyticsBucket {
  date: string;
  shootingRounds: number;
  producedRounds: number;
  shootingSessions: number;
  notes?: string[];
}

export interface WorkshopAnalytics {
  from: string;
  to: string;
  shootingRounds: number;
  shootingSessions: number;
  trackedRounds: number;
  untrackedRounds: number;
  producedRounds: number;
  availableRounds: number;
  maintenanceEvents: number;
  malfunctionEvents: number;
  correctionCount: number;
  projectionState: WorkshopProjectionState;
  buckets: WorkshopAnalyticsBucket[];
}

export interface WorkshopComponentCondition {
  component: WorkshopPhysicalComponent;
  definition: WorkshopComponentDefinition;
  installation: WorkshopComponentInstallation;
}

export interface WorkshopRoundHistoryPoint {
  effectiveAt: string;
  delta: number;
  cumulativeRounds: number;
  operationId: string;
}

export interface WorkshopShootingSession {
  session: {
    id: string;
    accountId: string;
    firearmId: string;
    date: string;
    totalRounds: number;
    notes?: string | null;
    consumptionRecords: WorkshopConsumptionRequest[];
    usageSegments: WorkshopUsageSegmentRequest[];
    untrackedAmmunition: number;
    correctionOfId?: string | null;
  };
  superseded: boolean;
}

export interface WorkshopHistoryPage<T> {
  items: T[];
  page: number;
  pageSize: number;
  hasMore: boolean;
}

export interface WorkshopFirearmDetail {
  firearm: WorkshopFirearm;
  components: WorkshopComponentCondition[];
  installationHistory: WorkshopHistoryPage<WorkshopComponentCondition>;
  maintenance: WorkshopHistoryPage<WorkshopMaintenanceEvent>;
  shooting: WorkshopHistoryPage<WorkshopShootingSession>;
  roundHistory: WorkshopRoundHistoryPoint[];
  replacementCandidates: WorkshopPhysicalComponent[];
  projectionState: WorkshopProjectionState;
  malfunctionCount: number;
}
