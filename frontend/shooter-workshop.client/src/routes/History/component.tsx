import { PageTitle, EmptyState } from "@morwalpiz/layout";
import { getWorkshopHistory } from "@morwalpizvideo/services";
import { ActivityList, ResourceState, useResource } from "../shared";
import { useI18n } from "../../i18n";
export default function History() {
  const { t } = useI18n();
  const resource = useResource(getWorkshopHistory);
  return (
    <ResourceState resource={resource}>
      {(items) => (
        <div>
          <PageTitle
            title={t("History")}
            subtitle={t("Account-filtered chronological activity from the controller.")}
          />
          {items.length ? (
            <section className="panel">
              <ActivityList items={items} />
            </section>
          ) : (
            <EmptyState title={t("No history yet")} />
          )}
        </div>
      )}
    </ResourceState>
  );
}
