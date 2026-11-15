import { useState } from "react";
import { Link, useParams } from "react-router";
import { PageTitle, EmptyState } from "@morwalpiz/layout";
import type {
  CreateWorkshopFirearmRequest,
  WorkshopFirearm,
} from "@morwalpizvideo/models";
import {
  archiveWorkshopFirearm,
  getWorkshopFirearms,
  updateWorkshopFirearm,
} from "@morwalpizvideo/services";
import { WorkshopForm } from "../../components/WorkshopForm";
import { ResourceState, Stat, useResource } from "../shared";
import { useI18n } from "../../i18n";
import { formatNumber } from "../../i18n-formatters";
export default function FirearmDetail() {
  const { culture, t } = useI18n();
  const { id = "" } = useParams();
  const resource = useResource(getWorkshopFirearms);
  const [editing, setEditing] = useState(false);
  return (
    <ResourceState resource={resource}>
      {(items) => {
        const firearm = items.find((item) => item.id === id);
        if (!firearm)
          return (
            <EmptyState
              title={t("Firearm not found")}
              message={t("The record may be archived or belong to another account.")}
            />
          );
        return (
          <div>
            <PageTitle
              title={
                firearm.nickname || `${firearm.manufacturer} ${firearm.model}`
              }
              subtitle={`${firearm.caliber} · ${t(firearm.archived ? "archived" : "active")}`}
            />
            <div className="stats-grid">
              <Stat
                label="Total rounds"
                value={formatNumber(firearm.totalRounds, culture)}
              />
              <Stat label="Revision" value={firearm.revision} />
              <Stat label="Account record" value={firearm.id} />
            </div>
            {!firearm.archived && (
              <section className="panel my-3">
                <div className="section-heading">
                  <h2>{t("Manage firearm")}</h2>
                  <button
                    className="button button-quiet"
                    onClick={() => setEditing(!editing)}
                  >
                    {t(editing ? "Cancel" : "Edit details")}
                  </button>
                </div>
                {editing && (
                  <FirearmEditForm
                    firearm={firearm}
                    onSaved={() => {
                      setEditing(false);
                      resource.reload();
                    }}
                  />
                )}
                <button
                  className="button button-quiet"
                  onClick={async () => {
                    await archiveWorkshopFirearm(firearm.id, {
                      revision: firearm.revision,
                      idempotencyKey: crypto.randomUUID(),
                    });
                    resource.reload();
                  }}
                >
                  {t("Archive firearm")}
                </button>
              </section>
            )}
            <section className="panel">
              <h2>{t("Related workflows")}</h2>
              <p>{t("Record a session against this firearm or correct a previously recorded operation.")}</p>
              <Link className="button button-quiet" to="/sessions/shooting">
                {t("Record session")}
              </Link>
            </section>
          </div>
        );
      }}
    </ResourceState>
  );
}

function FirearmEditForm({
  firearm,
  onSaved,
}: {
  firearm: WorkshopFirearm;
  onSaved: () => void;
}) {
  const { t } = useI18n();
  const [form, setForm] = useState<CreateWorkshopFirearmRequest>({
    manufacturer: firearm.manufacturer,
    model: firearm.model,
    caliber: firearm.caliber,
    category: firearm.category,
    nickname: firearm.nickname,
    notes: firearm.notes,
  });
  return (
    <WorkshopForm
      title="Edit firearm"
      submitLabel="Save changes"
      onSubmit={async () => {
        await updateWorkshopFirearm(firearm.id, {
          ...form,
          revision: firearm.revision,
        });
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
        {t("Notes")}
        <textarea
          value={form.notes ?? ""}
          onChange={(event) => setForm({ ...form, notes: event.target.value })}
        />
      </label>
    </WorkshopForm>
  );
}
