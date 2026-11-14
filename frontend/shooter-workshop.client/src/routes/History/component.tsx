import { PageTitle, EmptyState } from "@morwalpiz/layout";
import { getWorkshopHistory } from "@morwalpizvideo/services";
import { ActivityList, ResourceState, useResource } from "../shared";
export default function History() {
  const resource = useResource(getWorkshopHistory);
  return (
    <ResourceState resource={resource}>
      {(items) => (
        <div>
          <PageTitle
            title="History"
            subtitle="Account-filtered chronological activity from the controller."
          />
          {items.length ? (
            <section className="panel">
              <ActivityList items={items} />
            </section>
          ) : (
            <EmptyState title="No history yet" />
          )}
        </div>
      )}
    </ResourceState>
  );
}
