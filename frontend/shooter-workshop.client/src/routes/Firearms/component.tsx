import { useState } from "react";
import { Link } from "react-router";
import { PageTitle, EmptyState } from "@morwalpiz/layout";
import type { CreateWorkshopFirearmRequest } from "@morwalpizvideo/models";
import {
  createWorkshopFirearm,
  getWorkshopFirearms,
} from "@morwalpizvideo/services";
import { WorkshopForm } from "../../components/WorkshopForm";
import { ResourceState, useResource } from "../shared";
export default function Firearms() {
  const resource = useResource(getWorkshopFirearms);
  const [showForm, setShowForm] = useState(false);
  return (
    <ResourceState resource={resource}>
      {(items) => (
        <div>
          <PageTitle
            title="My firearms"
            subtitle={`${items.length} account-owned records`}
          />
          <button
            className="button button-primary"
            onClick={() => setShowForm(!showForm)}
          >
            + Register firearm
          </button>
          {showForm && (
            <FirearmForm
              onSaved={() => {
                setShowForm(false);
                resource.reload();
              }}
            />
          )}
          {items.length ? (
            <div className="card-grid">
              {items.map((firearm) => (
                <Link
                  className="record-card"
                  to={`/firearms/${firearm.id}`}
                  key={firearm.id}
                >
                  <div className="record-card-top">
                    <span className="eyebrow">{firearm.caliber}</span>
                    <span className="badge">
                      {firearm.archived ? "archived" : "active"}
                    </span>
                  </div>
                  <h2>
                    {firearm.nickname ||
                      `${firearm.manufacturer} ${firearm.model}`}
                  </h2>
                  <p>
                    {firearm.manufacturer} {firearm.model}
                  </p>
                  <strong>{firearm.totalRounds.toLocaleString()} rounds</strong>
                </Link>
              ))}
            </div>
          ) : (
            <EmptyState
              title="No firearms registered"
              message="Add a firearm to connect sessions, components and allocations."
            />
          )}
        </div>
      )}
    </ResourceState>
  );
}

function FirearmForm({ onSaved }: { onSaved: () => void }) {
  const [form, setForm] = useState<CreateWorkshopFirearmRequest>({
    manufacturer: "",
    model: "",
    caliber: "",
    nickname: "",
    category: "",
    notes: "",
  });
  return (
    <WorkshopForm
      title="Register firearm"
      submitLabel="Save firearm"
      onSubmit={async () => {
        await createWorkshopFirearm(form);
        onSaved();
      }}
    >
      <div className="form-grid">
        {(
          ["manufacturer", "model", "caliber", "nickname", "category"] as const
        ).map((field) => (
          <label key={field}>
            {field}
            <input
              required={
                field === "manufacturer" ||
                field === "model" ||
                field === "caliber"
              }
              value={form[field] ?? ""}
              onChange={(event) =>
                setForm({ ...form, [field]: event.target.value })
              }
            />
          </label>
        ))}
      </div>
      <label>
        Notes
        <textarea
          value={form.notes ?? ""}
          onChange={(event) => setForm({ ...form, notes: event.target.value })}
        />
      </label>
    </WorkshopForm>
  );
}
