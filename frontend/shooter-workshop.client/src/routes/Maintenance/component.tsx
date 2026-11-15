import { useState } from "react";
import { useFetcher, useLoaderData } from "react-router";
import { PageTitle, EmptyState } from "@morwalpiz/layout";
import type {
  CreateWorkshopMaintenanceRequest,
  WorkshopComponentDefinition,
  WorkshopFirearm,
  WorkshopPhysicalComponent,
} from "@morwalpizvideo/models";
import type { action } from "./action";
import { useI18n } from "../../i18n";
import { formatNumber, localDateTimeInputValue } from "../../i18n-formatters";

export default function Maintenance() {
  const { culture, t } = useI18n();
  const { firearms: firearmItems, definitions: definitionItems, inventory: items } = useLoaderData() as {
    firearms: WorkshopFirearm[];
    definitions: WorkshopComponentDefinition[];
    inventory: WorkshopPhysicalComponent[];
  };
  const fetcher = useFetcher<typeof action>();
  const [targetType, setTargetType] = useState<"firearm" | "component">("firearm");
  const [form, setForm] = useState<CreateWorkshopMaintenanceRequest>({
    firearmId: null,
    componentId: null,
    eventType: "Inspection",
    date: new Date().toISOString(),
    description: "",
    idempotencyKey: "",
  });
  const localDateTime = localDateTimeInputValue(new Date(form.date));

  return (
    <div>
      <PageTitle title={t("Maintenance")} subtitle={t("Component projections and service intervals.")} />
      <section className="panel">
        <fetcher.Form method="post" className="panel form-panel">
          <h2>{t("Record maintenance")}</h2>
          <label>
            {t("Target")}
            <select
              name="targetType"
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
              name="targetId"
              value={(targetType === "firearm" ? form.firearmId : form.componentId) ?? ""}
              onChange={(event) =>
                setForm({
                  ...form,
                  firearmId: targetType === "firearm" ? event.target.value || null : null,
                  componentId: targetType === "component" ? event.target.value || null : null,
                })
              }
            >
              <option value="">{t(targetType === "firearm" ? "Select firearm" : "Select component")}</option>
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
                name="eventType"
                value={form.eventType}
                onChange={(event) => setForm({
                  ...form,
                  eventType: event.target.value as CreateWorkshopMaintenanceRequest["eventType"],
                })}
              >
                <option value="Inspection">{t("Inspection")}</option>
                <option value="Cleaning">{t("Cleaning")}</option>
                <option value="ComponentMaintenance">{t("Component maintenance")}</option>
                <option value="Replacement">{t("Replacement")}</option>
                <option value="Malfunction">{t("Malfunction")}</option>
                <option value="General">{t("General")}</option>
              </select>
            </label>
            <label>
              {t("Effective date and time")}
              <input
                type="datetime-local"
                required
                name="date"
                value={localDateTime || localDateTimeInputValue()}
                onChange={(event) => setForm({ ...form, date: new Date(event.target.value).toISOString() })}
              />
            </label>
          </div>
          <label>
            {t("Description")}
            <textarea name="description" required value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} />
          </label>
          <input type="hidden" name="notes" value={form.notes ?? ""} />
          {fetcher.state === "idle" && fetcher.data && !fetcher.data.ok && (
            <p className="form-error" role="alert">{fetcher.data.error}</p>
          )}
          <button className="button button-primary" disabled={fetcher.state !== "idle"}>
            {fetcher.state === "submitting" ? t("Saving…") : t("Save event")}
          </button>
        </fetcher.Form>
        {items.length ? (
          <ul className="compact-list">
            {items.map((item) => (
              <li key={item.id}>
                <span>
                  <strong>{componentLabel(item, definitionItems)}</strong>
                  <small>{formatNumber(item.totalRounds, culture)} {t("rounds")} · {t("interval {value}", { value: item.serviceInterval ?? t("manual") })}</small>
                </span>
                <span className="badge">{t(item.status)}</span>
              </li>
            ))}
          </ul>
        ) : <EmptyState title={t("No tracked components")} />}
      </section>
    </div>
  );
}

function componentLabel(item: WorkshopPhysicalComponent, definitions: WorkshopComponentDefinition[]): string {
  return definitions.find((definition) => definition.id === item.definitionId)?.name ?? item.id;
}
