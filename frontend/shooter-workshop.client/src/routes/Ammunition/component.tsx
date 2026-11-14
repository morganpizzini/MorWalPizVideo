import { PageTitle, EmptyState } from "@morwalpiz/layout";
import { getWorkshopBatches } from "@morwalpizvideo/services";
import { ResourceState, useResource } from "../shared";
import { useI18n } from "../../i18n";
import { formatNumber } from "../../i18n-formatters";
export default function Ammunition() {
  const { culture, t } = useI18n();
  const resource = useResource(getWorkshopBatches);
  return (
    <ResourceState resource={resource}>
      {(items) => (
        <div>
          <PageTitle
            title={t("Ammunition")}
            subtitle={t("Produced batches and current on-hand stock.")}
          />
          {items.length ? (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>{t("Batch")}</th>
                    <th>{t("Caliber")}</th>
                    <th>{t("Produced")}</th>
                    <th>{t("On hand")}</th>
                    <th>{t("Revision")}</th>
                  </tr>
                </thead>
                <tbody>
                  {items.map((batch) => (
                    <tr key={batch.id}>
                      <td>{batch.id}</td>
                      <td>{batch.recipeSnapshot.caliber}</td>
                      <td>{formatNumber(batch.producedQuantity, culture)}</td>
                      <td>
                        <strong>{formatNumber(batch.onHandQuantity, culture)}</strong>
                      </td>
                      <td>{batch.revision}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <EmptyState title={t("No ammunition batches")} />
          )}
        </div>
      )}
    </ResourceState>
  );
}
