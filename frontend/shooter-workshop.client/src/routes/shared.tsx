import { useEffect, useState, type ReactNode } from "react";
import { EmptyState } from "@morwalpiz/layout";
import type { WorkshopActivity } from "@morwalpizvideo/models";
import { ErrorState, LoadingState } from "../components/States";
import { useI18n } from "../i18n";
import { formatDate } from "../i18n-formatters";

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
  return (
    <div className="stat">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

export function ActivityList({ items }: { items: WorkshopActivity[] }) {
  const { culture } = useI18n();
  return (
    <ul className="activity-list">
      {items.map((item) => (
        <li key={item.id}>
          <span className="activity-dot" />
          <div>
            <strong>{item.type}</strong>
            <p>Operation {item.operationId}</p>
          </div>
          <time>{formatDate(item.effectiveAt, culture)}</time>
        </li>
      ))}
    </ul>
  );
}
