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
import { useI18n } from "../../i18n";
import { formatNumber } from "../../i18n-formatters";
export default function Firearms() {
  const { culture, t } = useI18n();
  const resource = useResource(getWorkshopFirearms);
  const [showForm, setShowForm] = useState(false);
  return (
    <ResourceState resource={resource}>
      {(items) => (
        <div>
          <PageTitle
            title={t("My firearms")}
            subtitle={t("{count} account-owned records", { count: items.length })}
          />
          <button
            className="button button-primary"
            onClick={() => setShowForm(!showForm)}
          >
            + {t("Register firearm")}
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
            <div className="card-grid mt-3">
              {items.map((firearm) => (
                <Link
                  className="record-card"
                  to={`/firearms/${firearm.id}`}
                  key={firearm.id}
                >
                  <div className="record-card-top">
                    <span className="eyebrow">{firearm.caliber}</span>
                    <span className="badge">
                      {t(firearm.archived ? "archived" : "active")}
                    </span>
                  </div>
                  <h2>
                    {firearm.nickname ||
                      `${firearm.manufacturer} ${firearm.model}`}
                  </h2>
                  <p>
                    {firearm.manufacturer} {firearm.model}
                  </p>
                  <strong>{formatNumber(firearm.totalRounds, culture)} {t("rounds")}</strong>
                </Link>
              ))}
            </div>
          ) : (
            <EmptyState
              title={t("No firearms registered")}
              message={t("Add a firearm to connect sessions, components and allocations.")}
            />
          )}
        </div>
      )}
    </ResourceState>
  );
}

function FirearmForm({ onSaved }: { onSaved: () => void }) {
  const { t } = useI18n();
  const [form, setForm] = useState<CreateWorkshopFirearmRequest>({
    manufacturer: "",
    model: "",
    caliber: "",
    initialRounds: 0,
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
            {t(field)}
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
        {t("Initial rounds")}
        <input
          type="number"
          min="0"
          value={form.initialRounds ?? 0}
          onChange={(event) =>
            setForm({ ...form, initialRounds: Number(event.target.value) })
          }
        />
      </label>
      <label>
        {t("Notes")}
        <textarea
          value={form.notes ?? ""}
          onChange={(event) => setForm({ ...form, notes: event.target.value })}
        />
      </label>
    </WorkshopForm>
  );
}
