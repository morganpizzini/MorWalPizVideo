import { useState } from "react";
import { PageTitle, EmptyState } from "@morwalpiz/layout";
import type { CreateWorkshopMaintenanceRequest } from "@morwalpizvideo/models";
import {
  getWorkshopComponents,
  recordWorkshopMaintenance,
} from "@morwalpizvideo/services";
import { WorkshopForm } from "../../components/WorkshopForm";
import { ResourceState, useResource } from "../shared";
import { useI18n } from "../../i18n";
import { formatNumber, localDateInputValue } from "../../i18n-formatters";
export default function Maintenance() {
  const { culture, t } = useI18n();
  const inventory = useResource(getWorkshopComponents);
  const [form, setForm] = useState<CreateWorkshopMaintenanceRequest>({
    componentId: null,
    eventType: "Inspection",
    date: localDateInputValue(),
    description: "",
    roundCountAtEvent: null,
    idempotencyKey: "",
  });
  return (
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
                  idempotencyKey: crypto.randomUUID(),
                });
                setForm({ ...form, description: "" });
              }}
            >
              <label>
                {t("Component")}
                <select
                  required
                  value={form.componentId ?? ""}
                  onChange={(event) =>
                    setForm({
                      ...form,
                      componentId: event.target.value || null,
                    })
                  }
                >
                  <option value="">{t("Select component")}</option>
                  {items.map((item) => (
                    <option value={item.id} key={item.id}>
                      {item.id} · {t(item.status)}
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
                {t("Round count")}
                <input
                  type="number"
                  min="0"
                  value={form.roundCountAtEvent ?? ""}
                  onChange={(event) =>
                    setForm({
                      ...form,
                      roundCountAtEvent: event.target.value
                        ? Number(event.target.value)
                        : null,
                    })
                  }
                />
              </label>
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
  );
}
