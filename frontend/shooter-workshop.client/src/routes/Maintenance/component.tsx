import { useState } from "react";
import { PageTitle, EmptyState } from "@morwalpiz/layout";
import type { CreateWorkshopMaintenanceRequest } from "@morwalpizvideo/models";
import {
  getWorkshopComponents,
  recordWorkshopMaintenance,
} from "@morwalpizvideo/services";
import { WorkshopForm } from "../../components/WorkshopForm";
import { ResourceState, useResource } from "../shared";
export default function Maintenance() {
  const inventory = useResource(getWorkshopComponents);
  const [form, setForm] = useState<CreateWorkshopMaintenanceRequest>({
    componentId: null,
    eventType: "Inspection",
    date: new Date().toISOString().slice(0, 10),
    description: "",
    roundCountAtEvent: null,
    idempotencyKey: "",
  });
  return (
    <ResourceState resource={inventory}>
      {(items) => (
        <div>
          <PageTitle
            title="Maintenance"
            subtitle="Component projections and service intervals."
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
                Component
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
                  <option value="">Select component</option>
                  {items.map((item) => (
                    <option value={item.id} key={item.id}>
                      {item.id} · {item.status}
                    </option>
                  ))}
                </select>
              </label>
              <div className="form-grid">
                <label>
                  Event type
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
                    <option value="Inspection">Inspection</option>
                    <option value="Cleaning">Cleaning</option>
                    <option value="ComponentMaintenance">
                      Component maintenance
                    </option>
                    <option value="Replacement">Replacement</option>
                    <option value="General">General</option>
                  </select>
                </label>
                <label>
                  Date
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
                Round count
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
                Description
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
                        {item.totalRounds.toLocaleString()} rounds · interval{" "}
                        {item.serviceInterval ?? "manual"}
                      </small>
                    </span>
                    <span className="badge">{item.status}</span>
                  </li>
                ))}
              </ul>
            ) : (
              <EmptyState title="No tracked components" />
            )}
          </section>
        </div>
      )}
    </ResourceState>
  );
}
