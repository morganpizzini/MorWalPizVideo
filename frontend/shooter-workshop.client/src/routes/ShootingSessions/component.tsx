import { useState } from "react";
import { PageTitle } from "@morwalpiz/layout";
import type {
  WorkshopConsumptionRequest,
  WorkshopFirearm,
} from "@morwalpizvideo/models";
import {
  correctWorkshopShooting,
  getWorkshopFirearms,
  recordWorkshopShooting,
} from "@morwalpizvideo/services";
import { WorkshopForm } from "../../components/WorkshopForm";
import { ResourceState, useResource } from "../shared";
import { useI18n } from "../../i18n";
export default function ShootingSessions() {
  const { t } = useI18n();
  const firearms = useResource(getWorkshopFirearms);
  const [form, setForm] = useState({
    firearmId: "",
    rounds: "0",
    untracked: "0",
    consumptionRecords: [] as WorkshopConsumptionRequest[],
  });
  return (
    <div>
      <PageTitle
        title={t("Shooting sessions")}
        subtitle={t("Record new sessions or correct an operation without rewriting history.")}
      />
      <ResourceState resource={firearms}>
        {(items) => (
          <>
            <WorkshopForm
              title="Record shooting"
              submitLabel="Record session"
              onSubmit={async () => {
                const totalRounds = Number(form.rounds);
                await recordWorkshopShooting({
                  firearmId: form.firearmId,
                  date: new Date().toISOString(),
                  totalRounds,
                  untrackedAmmunition: Number(form.untracked),
                  consumptionRecords: form.consumptionRecords.filter((record) => record.quantity > 0),
                  usageSegments: [],
                  idempotencyKey: crypto.randomUUID(),
                });
              }}
            >
              <label>
                {t("Firearm")}
                <select
                  required
                  value={form.firearmId}
                  onChange={(event) =>
                    setForm({ ...form, firearmId: event.target.value })
                  }
                >
                  <option value="">{t("Select firearm")}</option>
                  {items
                    .filter((firearm) => !firearm.archived)
                    .map((firearm) => (
                      <option value={firearm.id} key={firearm.id}>
                        {firearm.nickname ||
                          `${firearm.manufacturer} ${firearm.model}`}
                      </option>
                    ))}
                </select>
              </label>
              <div className="form-grid">
                <label>
                  {t("Total rounds")}
                  <input
                    type="number"
                    min="1"
                    required
                    value={form.rounds}
                    onChange={(event) =>
                      setForm({ ...form, rounds: event.target.value })
                    }
                  />
                </label>
                <label>
                  {t("Untracked ammunition")}
                  <input
                    type="number"
                    min="0"
                    required
                    value={form.untracked}
                    onChange={(event) =>
                      setForm({ ...form, untracked: event.target.value })
                    }
                  />
                </label>
              </div>
              <ShootingConsumptionFields
                records={form.consumptionRecords}
                onChange={(consumptionRecords) => setForm({ ...form, consumptionRecords })}
              />
            </WorkshopForm>
            <ShootingCorrectionForm
              firearms={items.filter((firearm) => !firearm.archived)}
            />
          </>
        )}
      </ResourceState>
    </div>
  );
}

function ShootingConsumptionFields({
  records,
  onChange,
}: {
  records: WorkshopConsumptionRequest[];
  onChange: (records: WorkshopConsumptionRequest[]) => void;
}) {
  const { t } = useI18n();
  return (
    <fieldset>
      <legend>{t("Ammunition records")}</legend>
      {records.map((record, index) => (
        <div className="form-grid" key={index}>
          <label>
            {t("Batch ID")}
            <input
              value={record.batchId ?? ""}
              onChange={(event) => {
                const next = [...records];
                next[index] = { ...record, batchId: event.target.value || null };
                onChange(next);
              }}
            />
          </label>
          <label>
            {t("Allocation ID")}
            <input
              value={record.allocationId ?? ""}
              onChange={(event) => {
                const next = [...records];
                next[index] = { ...record, allocationId: event.target.value || null };
                onChange(next);
              }}
            />
          </label>
          <label>
            {t("Quantity")}
            <input
              type="number"
              min="1"
              required
              value={record.quantity || ""}
              onChange={(event) => {
                const next = [...records];
                next[index] = { ...record, quantity: Number(event.target.value) };
                onChange(next);
              }}
            />
          </label>
          <button type="button" className="button button-quiet" onClick={() => onChange(records.filter((_, itemIndex) => itemIndex !== index))}>
            {t("Remove")}
          </button>
        </div>
      ))}
      <button type="button" className="button button-quiet" onClick={() => onChange([...records, { quantity: 0 }])}>
        + {t("Ammunition record")}
      </button>
    </fieldset>
  );
}

function ShootingCorrectionForm({ firearms }: { firearms: WorkshopFirearm[] }) {
  const { t } = useI18n();
  const [operationId, setOperationId] = useState("");
  const [firearmId, setFirearmId] = useState("");
  const [rounds, setRounds] = useState("0");
  return (
    <WorkshopForm
      title="Correct shooting operation"
      submitLabel="Record correction"
      onSubmit={async () => {
        await correctWorkshopShooting({
          originalOperationId: operationId,
          replacementShooting: {
            firearmId,
            date: new Date().toISOString(),
            totalRounds: Number(rounds),
            untrackedAmmunition: Number(rounds),
            consumptionRecords: [],
            usageSegments: [],
            idempotencyKey: crypto.randomUUID(),
          },
          idempotencyKey: crypto.randomUUID(),
        });
      }}
    >
      <label>
        {t("Original operation ID")}
        <input
          required
          value={operationId}
          onChange={(event) => setOperationId(event.target.value)}
        />
      </label>
      <div className="form-grid">
        <label>
          {t("Correct firearm")}
          <select
            required
            value={firearmId}
            onChange={(event) => setFirearmId(event.target.value)}
          >
            <option value="">{t("Select firearm")}</option>
            {firearms.map((firearm) => (
              <option value={firearm.id} key={firearm.id}>
                {firearm.nickname || `${firearm.manufacturer} ${firearm.model}`}
              </option>
            ))}
          </select>
        </label>
        <label>
          {t("Correct total rounds")}
          <input
            type="number"
            min="1"
            required
            value={rounds}
            onChange={(event) => setRounds(event.target.value)}
          />
        </label>
      </div>
      <small>{t("A correction appends a compensating operation; the original remains in history.")}</small>
    </WorkshopForm>
  );
}
