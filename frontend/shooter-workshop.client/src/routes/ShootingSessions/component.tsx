import { useState } from "react";
import { PageTitle } from "@morwalpiz/layout";
import type { WorkshopFirearm } from "@morwalpizvideo/models";
import {
  correctWorkshopShooting,
  getWorkshopFirearms,
  recordWorkshopShooting,
} from "@morwalpizvideo/services";
import { WorkshopForm } from "../../components/WorkshopForm";
import { ResourceState, useResource } from "../shared";
export default function ShootingSessions() {
  const firearms = useResource(getWorkshopFirearms);
  const [form, setForm] = useState({
    firearmId: "",
    rounds: "0",
    untracked: "0",
    consumption: "[]",
    usage: "[]",
  });
  return (
    <div>
      <PageTitle
        title="Shooting sessions"
        subtitle="Record new sessions or correct an operation without rewriting history."
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
                  consumptionRecords: JSON.parse(form.consumption),
                  usageSegments: JSON.parse(form.usage),
                  idempotencyKey: crypto.randomUUID(),
                });
              }}
            >
              <label>
                Firearm
                <select
                  required
                  value={form.firearmId}
                  onChange={(event) =>
                    setForm({ ...form, firearmId: event.target.value })
                  }
                >
                  <option value="">Select firearm</option>
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
                  Total rounds
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
                  Untracked ammunition
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
              <label>
                Consumption records JSON
                <textarea
                  value={form.consumption}
                  onChange={(event) =>
                    setForm({ ...form, consumption: event.target.value })
                  }
                />
              </label>
              <label>
                Usage segments JSON
                <textarea
                  value={form.usage}
                  onChange={(event) =>
                    setForm({ ...form, usage: event.target.value })
                  }
                />
              </label>
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

function ShootingCorrectionForm({ firearms }: { firearms: WorkshopFirearm[] }) {
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
        Original operation ID
        <input
          required
          value={operationId}
          onChange={(event) => setOperationId(event.target.value)}
        />
      </label>
      <div className="form-grid">
        <label>
          Correct firearm
          <select
            required
            value={firearmId}
            onChange={(event) => setFirearmId(event.target.value)}
          >
            <option value="">Select firearm</option>
            {firearms.map((firearm) => (
              <option value={firearm.id} key={firearm.id}>
                {firearm.nickname || `${firearm.manufacturer} ${firearm.model}`}
              </option>
            ))}
          </select>
        </label>
        <label>
          Correct total rounds
          <input
            type="number"
            min="1"
            required
            value={rounds}
            onChange={(event) => setRounds(event.target.value)}
          />
        </label>
      </div>
      <small>
        A correction appends a compensating operation; the original remains in
        history.
      </small>
    </WorkshopForm>
  );
}
