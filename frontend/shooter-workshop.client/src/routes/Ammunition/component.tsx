import { PageTitle, EmptyState } from "@morwalpiz/layout";
import { getWorkshopBatches } from "@morwalpizvideo/services";
import { ResourceState, useResource } from "../shared";
export default function Ammunition() {
  const resource = useResource(getWorkshopBatches);
  return (
    <ResourceState resource={resource}>
      {(items) => (
        <div>
          <PageTitle
            title="Ammunition"
            subtitle="Produced batches and current on-hand stock."
          />
          {items.length ? (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Batch</th>
                    <th>Caliber</th>
                    <th>Produced</th>
                    <th>On hand</th>
                    <th>Revision</th>
                  </tr>
                </thead>
                <tbody>
                  {items.map((batch) => (
                    <tr key={batch.id}>
                      <td>{batch.id}</td>
                      <td>{batch.recipeSnapshot.caliber}</td>
                      <td>{batch.producedQuantity}</td>
                      <td>
                        <strong>{batch.onHandQuantity}</strong>
                      </td>
                      <td>{batch.revision}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <EmptyState title="No ammunition batches" />
          )}
        </div>
      )}
    </ResourceState>
  );
}
