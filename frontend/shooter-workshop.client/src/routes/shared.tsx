import { useEffect, useState, type ReactNode } from "react";
import { EmptyState } from "@morwalpiz/layout";
import type { WorkshopActivity } from "@morwalpizvideo/models";
import { ErrorState, LoadingState } from "../components/States";
import { useI18n } from "../i18n";
import { formatDate, formatDateTime } from "../i18n-formatters";

export function useResource<T>(load: () => Promise<T>) {
  const [value, setValue] = useState<T | null>(null);
  const [error, setError] = useState<unknown>(null);
  const [loading, setLoading] = useState(true);
  const reload = () => {
    setLoading(true);
    setError(null);
    void load()
      .then(setValue)
      .catch(setError)
      .finally(() => setLoading(false));
  };
  useEffect(reload, []);
  return { value, error, loading, reload };
}

export function ResourceState<T>({
  resource,
  children,
}: {
  resource: ReturnType<typeof useResource<T>>;
  children: (value: T) => ReactNode;
}) {
  const { t } = useI18n();
  if (resource.loading) return <LoadingState />;
  if (resource.error)
    return <ErrorState error={resource.error} onRetry={resource.reload} />;
  return resource.value ? (
    children(resource.value)
  ) : (
    <EmptyState
      title={t("No data returned")}
      message={t("The API returned an empty response.")}
    />
  );
}

export function Stat({
  label,
  value,
}: {
  label: string;
  value: string | number;
}) {
  const { t } = useI18n();
  return (
    <div className="stat">
      <span>{t(label)}</span>
      <strong>{value}</strong>
    </div>
  );
}

const operationTitles: Record<string, string> = {
  FirearmCreated: "Firearm registered",
  FirearmUpdated: "Firearm details updated",
  FirearmArchived: "Firearm archived",
  ComponentDefinitionCreated: "Component definition created",
  ComponentDefinitionUpdated: "Component definition updated",
  ComponentDefinitionArchived: "Component definition archived",
  ComponentDefinitionDeleted: "Component definition deleted",
  ComponentCreated: "Component added",
  ComponentUpdated: "Component updated",
  ComponentArchived: "Component archived",
  ComponentDeleted: "Component deleted",
  ComponentInstalled: "Component installed",
  ComponentRemoved: "Component removed",
  MaintenanceRecorded: "Maintenance recorded",
  CounterAdjusted: "Counter adjusted",
  RecipeCreated: "Recipe created",
  RecipeUpdated: "Recipe updated",
  RecipeArchived: "Recipe archived",
  ProductionRecorded: "Production recorded",
  AllocationRecorded: "Ammunition allocated",
  AllocationReleased: "Ammunition allocation released",
  AllocationReallocated: "Ammunition reallocated",
  ShootingRecorded: "Shooting session recorded",
  ShootingCorrected: "Shooting session corrected",
  StockAdjusted: "Stock adjusted",
  ComponentSwapped: "Component swapped",
};

const numericOperationTypes = [
  "FirearmCreated",
  "FirearmUpdated",
  "FirearmArchived",
  "ComponentDefinitionCreated",
  "ComponentDefinitionUpdated",
  "ComponentDefinitionArchived",
  "ComponentDefinitionDeleted",
  "ComponentCreated",
  "ComponentUpdated",
  "ComponentArchived",
  "ComponentDeleted",
  "ComponentInstalled",
  "ComponentRemoved",
  "MaintenanceRecorded",
  "CounterAdjusted",
  "RecipeCreated",
  "RecipeUpdated",
  "RecipeArchived",
  "ProductionRecorded",
  "AllocationRecorded",
  "AllocationReleased",
  "AllocationReallocated",
  "ShootingRecorded",
  "ShootingCorrected",
  "StockAdjusted",
  "ComponentSwapped",
] as const;

function operationTypeName(type: unknown): string | null {
  if (typeof type === "string") {
    const trimmed = type.trim();
    if (trimmed && operationTitles[trimmed]) return trimmed;
    if (/^\d+$/.test(trimmed)) return numericOperationTypes[Number(trimmed)] ?? null;
  } else if (typeof type === "number" && Number.isInteger(type)) {
    return numericOperationTypes[type] ?? null;
  }
  return null;
}

export function getActivityTitle(type: unknown): string {
  const name = operationTypeName(type);
  return name ? operationTitles[name] : "Workshop activity";
}

export function ActivityList({ items, compact = false }: { items: WorkshopActivity[]; compact?: boolean }) {
  const { culture, t } = useI18n();
  return (
    <ul className={`activity-list${compact ? " activity-list-compact" : ""}`}>
      {items.map((item) => (
        <li key={item.id}>
          <span className="activity-dot" />
          <div>
            <strong>{getActivityTitle(item.type)}</strong>
            {item.entityLabel ? <p>{item.entityLabel}</p> : !compact ? <p>{t("Activity recorded")}</p> : null}
            {!compact ? (
              <>
                <small>
                  {t("Operation {id}", {
                    id:
                      typeof item.operationId === "string" && item.operationId.trim()
                        ? item.operationId
                        : t("not set"),
                  })}
                </small>&nbsp;
                <small>
                  {t("Effective {date}", { date: formatDateTime(item.effectiveAt, culture) })}
                  {item.recordedAt && ` · ${t("recorded {date}", { date: formatDateTime(item.recordedAt, culture) })}`}
                </small>
              </>
            ) : null}
          </div>
          <time dateTime={item.effectiveAt}>{compact ? formatDateTime(item.effectiveAt, culture) : formatDate(item.effectiveAt, culture)}</time>
        </li>
      ))}
    </ul>
  );
}
