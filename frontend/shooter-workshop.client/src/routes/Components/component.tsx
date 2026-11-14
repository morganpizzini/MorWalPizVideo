import { useState } from "react";
import { PageTitle, EmptyState } from "@morwalpiz/layout";
import type {
  WorkshopComponentDefinition,
  WorkshopFirearm,
  WorkshopPhysicalComponent,
} from "@morwalpizvideo/models";
import {
  createAndInstallWorkshopComponent,
  createWorkshopComponentDefinition,
  getWorkshopComponentDefinitions,
  getWorkshopComponents,
  getWorkshopFirearms,
} from "@morwalpizvideo/services";
import { WorkshopForm } from "../../components/WorkshopForm";
import { ResourceState, useResource } from "../shared";
import { useI18n } from "../../i18n";
import { formatNumber } from "../../i18n-formatters";
export default function Components() {
  const { t } = useI18n();
  const definitions = useResource(getWorkshopComponentDefinitions);
  const inventory = useResource(getWorkshopComponents);
  const firearms = useResource(getWorkshopFirearms);
  return (
    <div>
      <PageTitle
        title={t("Components")}
        subtitle={t("Catalogue definitions and independent physical inventory.")}
      />
      <div className="detail-grid">
        <ResourceState resource={definitions}>
          {(items) => (
            <Catalogue             items={items} onSaved={definitions.reload} />
          )}
        </ResourceState>
        <ResourceState resource={inventory}>
          {(items) => (
            <Inventory
              items={items}
              definitions={definitions.value ?? []}
              firearms={firearms.value ?? []}
              onSaved={inventory.reload}
            />
          )}
        </ResourceState>
      </div>
    </div>
  );
}

function Catalogue({
  items,
  onSaved,
}: {
  items: WorkshopComponentDefinition[];
  onSaved: () => void;
}) {
  const { t } = useI18n();
  const [showForm, setShowForm] = useState(false);
  const [name, setName] = useState("");
  const [tags, setTags] = useState("");
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
              compatibilityTags: tags
                .split(",")
                .map((tag) => tag.trim())
                .filter(Boolean),
            });
            setShowForm(false);
            setName("");
            setTags("");
            onSaved();
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
          <label>
            {t("Compatibility tags")}
            <input
              value={tags}
              placeholder={t("Comma-separated tags")}
              onChange={(event) => setTags(event.target.value)}
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
                  {item.compatibilityTags.length
                    ? ` · ${item.compatibilityTags.join(", ")}`
                    : ""}
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
  firearms,
  onSaved,
}: {
  items: WorkshopPhysicalComponent[];
  definitions: WorkshopComponentDefinition[];
  firearms: WorkshopFirearm[];
  onSaved: () => void;
}) {
  const { culture, t } = useI18n();
  const [definitionId, setDefinitionId] = useState("");
  const [firearmId, setFirearmId] = useState("");
  const [note, setNote] = useState("");
  const [tagFilter, setTagFilter] = useState("");
  const normalizedFilter = tagFilter.trim().toLowerCase();
  const visibleItems = normalizedFilter
    ? items.filter((item) => {
        const definition = definitions.find(
          (candidate) => candidate.id === item.definitionId,
        );
        return definition?.compatibilityTags.some(
          (tag) => tag.toLowerCase() === normalizedFilter,
        );
      })
    : items;
  return (
    <section className="panel">
      <div className="section-heading">
        <h2>{t("My inventory")}</h2>
      </div>
      <label>
        {t("Filter by compatibility tag")}
        <input
          value={tagFilter}
          onChange={(event) => setTagFilter(event.target.value)}
          placeholder={t("e.g. CZ")}
        />
      </label>
      <WorkshopForm
        title="Add physical component"
        submitLabel="Add component"
        onSubmit={async () => {
          await createAndInstallWorkshopComponent({
            definitionId,
            firearmId,
            installedAt: new Date().toISOString(),
            identificationNote: note,
            idempotencyKey: crypto.randomUUID(),
          });
          setDefinitionId("");
          setFirearmId("");
          setNote("");
          onSaved();
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
          {t("Firearm")}
          <select
            required
            value={firearmId}
            onChange={(event) => setFirearmId(event.target.value)}
          >
            <option value="">{t("Select firearm")}</option>
            {firearms.filter((firearm) => !firearm.archived).map((firearm) => (
              <option key={firearm.id} value={firearm.id}>
                {firearm.nickname || `${firearm.manufacturer} ${firearm.model}`}
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
      {visibleItems.length ? (
        <ul className="compact-list">
          {visibleItems.map((item) => (
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
        <EmptyState
          title={items.length ? t("No components match this tag") : t("No physical components")}
        />
      )}
    </section>
  );
}
