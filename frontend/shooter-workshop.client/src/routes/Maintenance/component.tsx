import { useState } from "react";
import { PageTitle, EmptyState } from "@morwalpiz/layout";
import type {
  CreateWorkshopMaintenanceRequest,
  WorkshopComponentDefinition,
  WorkshopPhysicalComponent,
} from "@morwalpizvideo/models";
import {
  getWorkshopComponentDefinitions,
  getWorkshopComponents,
  getWorkshopFirearms,
  recordWorkshopMaintenance,
} from "@morwalpizvideo/services";
import { WorkshopForm } from "../../components/WorkshopForm";
import { ResourceState, useResource } from "../shared";
import { useI18n } from "../../i18n";
import { formatNumber, localDateInputValue } from "../../i18n-formatters";
export default function Maintenance() {
  const { culture, t } = useI18n();
  const inventory = useResource(getWorkshopComponents);
  const firearms = useResource(getWorkshopFirearms);
  const definitions = useResource(getWorkshopComponentDefinitions);
  const [targetType, setTargetType] = useState<"firearm" | "component">("firearm");
  const [form, setForm] = useState<CreateWorkshopMaintenanceRequest>({
    firearmId: null,
    componentId: null,
    eventType: "Inspection",
    date: localDateInputValue(),
    description: "",
    idempotencyKey: "",
  });
  return (
    <ResourceState resource={firearms}>
      {(firearmItems) => (
        <ResourceState resource={definitions}>
          {(definitionItems) => (
            <ResourceState resource={inventory}>
      {(items) => (
        <div>
          <PageTitle
            title={t("Maintenance")}
            subtitle={t("Component projections and service intervals.")}
          />
          <section className="panel">
            <WorkshopForm
              title="Record maintenance"
              submitLabel="Save event"
              onSubmit={async () => {
                await recordWorkshopMaintenance({
                  ...form,
                  firearmId: targetType === "firearm" ? form.firearmId : null,
                  componentId: targetType === "component" ? form.componentId : null,
                  idempotencyKey: crypto.randomUUID(),
                });
                setForm({ ...form, description: "", idempotencyKey: "" });
              }}
            >
              <label>
                {t("Target")}
                <select
                  value={targetType}
                  onChange={(event) => {
                    const nextType = event.target.value as "firearm" | "component";
                    setTargetType(nextType);
                    setForm({
                      ...form,
                      firearmId: nextType === "firearm" ? form.firearmId : null,
                      componentId: nextType === "component" ? form.componentId : null,
                    });
                  }}
                >
                  <option value="firearm">{t("Firearm")}</option>
                  <option value="component">{t("Component")}</option>
                </select>
              </label>
              <label>
                {t(targetType === "firearm" ? "Firearm" : "Component")}
                <select
                  required
                  value={(targetType === "firearm" ? form.firearmId : form.componentId) ?? ""}
                  onChange={(event) =>
                    setForm({
                      ...form,
                      firearmId: targetType === "firearm" ? event.target.value || null : null,
                      componentId: targetType === "component" ? event.target.value || null : null,
                    })
                  }
                >
                  <option value="">
                    {t(targetType === "firearm" ? "Select firearm" : "Select component")}
                  </option>
                  {targetType === "firearm"
                    ? firearmItems.filter((firearm) => !firearm.archived).map((firearm) => (
                        <option value={firearm.id} key={firearm.id}>
                          {firearm.nickname || `${firearm.manufacturer} ${firearm.model}`}
                        </option>
                      ))
                    : items.map((item) => (
                        <option value={item.id} key={item.id}>
                          {componentLabel(item, definitionItems)} · {t(item.status)}
                        </option>
                      ))}
                </select>
              </label>
              <div className="form-grid">
                <label>
                  {t("Event type")}
                  <select
                    value={form.eventType}
                    onChange={(event) =>
                      setForm({
                        ...form,
                        eventType: event.target
                          .value as CreateWorkshopMaintenanceRequest["eventType"],
                      })
                    }
                  >
                    <option value="Inspection">{t("Inspection")}</option>
                    <option value="Cleaning">{t("Cleaning")}</option>
                    <option value="ComponentMaintenance">
                      {t("Component maintenance")}
                    </option>
                    <option value="Replacement">{t("Replacement")}</option>
                    <option value="General">{t("General")}</option>
                  </select>
                </label>
                <label>
                  {t("Date")}
                  <input
                    type="date"
                    required
                    value={form.date}
                    onChange={(event) =>
                      setForm({ ...form, date: event.target.value })
                    }
                  />
                </label>
              </div>
              <label>
                {t("Description")}
                <textarea
                  required
                  value={form.description}
                  onChange={(event) =>
                    setForm({ ...form, description: event.target.value })
                  }
                />
              </label>
            </WorkshopForm>
            {items.length ? (
              <ul className="compact-list">
                {items.map((item) => (
                  <li key={item.id}>
                    <span>
                      <strong>{item.id}</strong>
                      <small>
                        {formatNumber(item.totalRounds, culture)} {t("rounds")} ·{" "}
                        {t("interval {value}", { value: item.serviceInterval ?? t("manual") })}
                      </small>
                    </span>
                    <span className="badge">{t(item.status)}</span>
                  </li>
                ))}
              </ul>
            ) : (
              <EmptyState title={t("No tracked components")} />
            )}
          </section>
        </div>
      )}
            </ResourceState>
          )}
        </ResourceState>
      )}
    </ResourceState>
  );
}

function componentLabel(
  item: WorkshopPhysicalComponent,
  definitions: WorkshopComponentDefinition[],
): string {
  return definitions.find((definition) => definition.id === item.definitionId)?.name ?? item.id;
}
