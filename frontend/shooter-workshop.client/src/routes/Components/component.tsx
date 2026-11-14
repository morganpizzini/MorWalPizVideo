import { useState } from "react";
import { PageTitle, EmptyState } from "@morwalpiz/layout";
import type {
  WorkshopComponentDefinition,
  WorkshopPhysicalComponent,
} from "@morwalpizvideo/models";
import {
  createWorkshopComponent,
  createWorkshopComponentDefinition,
  getWorkshopComponentDefinitions,
  getWorkshopComponents,
} from "@morwalpizvideo/services";
import { WorkshopForm } from "../../components/WorkshopForm";
import { ResourceState, useResource } from "../shared";
import { useI18n } from "../../i18n";
import { formatNumber } from "../../i18n-formatters";
export default function Components() {
  const { t } = useI18n();
  const definitions = useResource(getWorkshopComponentDefinitions);
  const inventory = useResource(getWorkshopComponents);
  return (
    <div>
      <PageTitle
        title={t("Components")}
        subtitle={t("Catalogue definitions and independent physical inventory.")}
      />
      <div className="detail-grid">
        <ResourceState resource={definitions}>
          {(items) => <Catalogue items={items} />}
        </ResourceState>
        <ResourceState resource={inventory}>
          {(items) => (
            <Inventory items={items} definitions={definitions.value ?? []} />
          )}
        </ResourceState>
      </div>
    </div>
  );
}

function Catalogue({ items }: { items: WorkshopComponentDefinition[] }) {
  const { t } = useI18n();
  const [showForm, setShowForm] = useState(false);
  const [name, setName] = useState("");
  return (
    <section className="panel">
      <div className="section-heading">
        <h2>{t("Catalogue")}</h2>
        <button
          className="button button-quiet"
          onClick={() => setShowForm(!showForm)}
        >
          + {t("Definition")}
        </button>
      </div>
      {showForm && (
        <WorkshopForm
          title="New component definition"
          submitLabel="Save definition"
          onSubmit={async () => {
            await createWorkshopComponentDefinition({
              name,
              trackingMode: "Counter",
            });
            setShowForm(false);
          }}
        >
          <label>
            {t("Name")}
            <input
              required
              value={name}
              onChange={(event) => setName(event.target.value)}
            />
          </label>
        </WorkshopForm>
      )}
      {items.length ? (
        <ul className="compact-list">
          {items.map((item) => (
            <li key={item.id}>
              <span>
                <strong>{item.name}</strong>
                <small>
                  {item.manufacturer ?? t("No manufacturer")} · {t(item.trackingMode)}{" "}
                  · {t("interval {value}", { value: item.defaultServiceInterval ?? t("not set") })}
                </small>
              </span>
              <span className="badge">
                {t(item.archived ? "archived" : "active")}
              </span>
            </li>
          ))}
        </ul>
      ) : (
        <EmptyState title={t("Catalogue is empty")} />
      )}
    </section>
  );
}

function Inventory({
  items,
  definitions,
}: {
  items: WorkshopPhysicalComponent[];
  definitions: WorkshopComponentDefinition[];
}) {
  const { culture, t } = useI18n();
  const [definitionId, setDefinitionId] = useState("");
  const [note, setNote] = useState("");
  return (
    <section className="panel">
      <div className="section-heading">
        <h2>{t("My inventory")}</h2>
      </div>
      <WorkshopForm
        title="Add physical component"
        submitLabel="Add component"
        onSubmit={async () => {
          await createWorkshopComponent({
            definitionId,
            identificationNote: note,
          });
          setDefinitionId("");
          setNote("");
        }}
      >
        <label>
          {t("Definition")}
          <select
            required
            value={definitionId}
            onChange={(event) => setDefinitionId(event.target.value)}
          >
            <option value="">{t("Select definition")}</option>
            {definitions.map((item) => (
              <option key={item.id} value={item.id}>
                {item.name}
              </option>
            ))}
          </select>
        </label>
        <label>
          {t("Identification note")}
          <input
            value={note}
            onChange={(event) => setNote(event.target.value)}
          />
        </label>
      </WorkshopForm>
      {items.length ? (
        <ul className="compact-list">
          {items.map((item) => (
            <li key={item.id}>
              <span>
                <strong>
                  {definitions.find(
                    (definition) => definition.id === item.definitionId,
                  )?.name ?? item.definitionId}
                </strong>
                <small>
                  {item.identificationNote || t("No identification note")} ·{" "}
                  {formatNumber(item.totalRounds, culture)} {t("rounds")}
                </small>
              </span>
              <span className="status">{t(item.status)}</span>
            </li>
          ))}
        </ul>
      ) : (
        <EmptyState title={t("No physical components")} />
      )}
    </section>
  );
}
