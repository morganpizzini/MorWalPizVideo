import { EmptyState } from "@morwalpiz/layout";
import type {
  WorkshopAmmunitionBatch,
  WorkshopProductionRecord,
} from "@morwalpizvideo/models";
import { useState } from "react";
import type { ReactNode } from "react";
import { useI18n } from "../../i18n";
import { formatNumber } from "../../i18n-formatters";

export default function AmmunitionPanel({
  batches,
  productionRecords,
  renderProductionEntry,
}: {
  batches: WorkshopAmmunitionBatch[];
  productionRecords: WorkshopProductionRecord[];
  renderProductionEntry: (
    record: WorkshopProductionRecord | undefined,
    minimumQuantity: number,
    onSaved: () => void,
    onCancel: () => void,
  ) => ReactNode;
}) {
  const { culture, t } = useI18n();
  const [editingRecordId, setEditingRecordId] = useState<string | null>(null);
  const editingRecord = productionRecords.find((record) => record.session.id === editingRecordId);
  const consumed = editingRecord
    ? batches.find((batch) => batch.productionSessionId === editingRecord.session.id)
    : undefined;
  const minimumQuantity = consumed
    ? Math.max(1, consumed.producedQuantity - consumed.onHandQuantity)
    : 1;

  return (
    <>
      {renderProductionEntry(
        editingRecord,
        minimumQuantity,
        () => setEditingRecordId(null),
        () => setEditingRecordId(null),
      )}
      <section className="panel">
        <h2>{t("Production records")}</h2>
        {productionRecords.length ? (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>{t("Production date")}</th>
                  <th>{t("Recipe")}</th>
                  <th>{t("Quantity produced")}</th>
                  <th>{t("Revision")}</th>
                  <th>{t("Corrections")}</th>
                  <th>{t("Actions")}</th>
                </tr>
              </thead>
              <tbody>
                {productionRecords.map(({ session, corrections }) => (
                  <tr key={session.id}>
                    <td>{new Date(session.productionDate).toLocaleDateString(culture)}</td>
                    <td>{session.recipeSnapshot.name}</td>
                    <td>{formatNumber(session.quantityProduced, culture)}</td>
                    <td>{session.revision}</td>
                    <td>{corrections.length}</td>
                    <td>
                      <button
                        type="button"
                        className="button button-quiet"
                        onClick={() => setEditingRecordId(session.id)}
                      >
                        {t("Edit")}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <EmptyState title={t("No production records")} />
        )}
      </section>
      <section className="panel">
        <h2>{t("Produced batches")}</h2>
        {batches.length ? (
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
                {batches.map((batch) => (
                  <tr key={batch.id}>
                    <td>{batch.id}</td>
                    <td>{batch.recipeSnapshot.caliber}</td>
                    <td>{formatNumber(batch.producedQuantity, culture)}</td>
                    <td><strong>{formatNumber(batch.onHandQuantity, culture)}</strong></td>
                    <td>{batch.revision}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <EmptyState title={t("No ammunition batches")} />
        )}
      </section>
    </>
  );
}
