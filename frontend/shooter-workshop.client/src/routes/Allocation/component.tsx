import { useState } from "react";
import { PageTitle } from "@morwalpiz/layout";
import {
  allocateWorkshopAmmunition,
  getWorkshopBatches,
  getWorkshopFirearms,
  releaseWorkshopAllocation,
  reallocateWorkshopAmmunition,
} from "@morwalpizvideo/services";
import { WorkshopForm } from "../../components/WorkshopForm";
import { ResourceState, useResource } from "../shared";
export default function Allocation() {
  const batches = useResource(getWorkshopBatches);
  const firearms = useResource(getWorkshopFirearms);
  const [form, setForm] = useState({
    batchId: "",
    firearmId: "",
    quantity: "0",
    notes: "",
  });
  const [allocationId, setAllocationId] = useState("");
  const [revision, setRevision] = useState("0");
  return (
    <div>
      <PageTitle
        title="Ammunition allocation"
        subtitle="Allocate, release, or move stock between firearms."
      />
      <ResourceState resource={batches}>
        {(batchItems) => (
          <ResourceState resource={firearms}>
            {(firearmItems) => (
              <>
                <WorkshopForm
                  title="Allocate stock"
                  submitLabel="Allocate"
                  onSubmit={async () => {
                    await allocateWorkshopAmmunition({
                      batchId: form.batchId,
                      firearmId: form.firearmId,
                      quantity: Number(form.quantity),
                      notes: form.notes,
                      idempotencyKey: crypto.randomUUID(),
                    });
                  }}
                >
                  <label>
                    Batch
                    <select
                      required
                      value={form.batchId}
                      onChange={(event) =>
                        setForm({ ...form, batchId: event.target.value })
                      }
                    >
                      <option value="">Select batch</option>
                      {batchItems.map((batch) => (
                        <option value={batch.id} key={batch.id}>
                          {batch.id} · {batch.onHandQuantity} on hand
                        </option>
                      ))}
                    </select>
                  </label>
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
                      {firearmItems
                        .filter((firearm) => !firearm.archived)
                        .map((firearm) => (
                          <option value={firearm.id} key={firearm.id}>
                            {firearm.nickname ||
                              `${firearm.manufacturer} ${firearm.model}`}
                          </option>
                        ))}
                    </select>
                  </label>
                  <label>
                    Quantity
                    <input
                      type="number"
                      min="1"
                      required
                      value={form.quantity}
                      onChange={(event) =>
                        setForm({ ...form, quantity: event.target.value })
                      }
                    />
                  </label>
                  <label>
                    Notes
                    <textarea
                      value={form.notes}
                      onChange={(event) =>
                        setForm({ ...form, notes: event.target.value })
                      }
                    />
                  </label>
                </WorkshopForm>
                <div className="detail-grid">
                  <WorkshopForm
                    title="Release allocation"
                    submitLabel="Release stock"
                    onSubmit={async () => {
                      await releaseWorkshopAllocation(allocationId, {
                        quantity: Number(form.quantity),
                        revision: Number(revision),
                        idempotencyKey: crypto.randomUUID(),
                      });
                    }}
                  >
                    <label>
                      Allocation ID
                      <input
                        required
                        value={allocationId}
                        onChange={(event) =>
                          setAllocationId(event.target.value)
                        }
                      />
                    </label>
                    <label>
                      Current revision
                      <input
                        type="number"
                        min="0"
                        required
                        value={revision}
                        onChange={(event) => setRevision(event.target.value)}
                      />
                    </label>
                    <label>
                      Quantity
                      <input
                        type="number"
                        min="1"
                        required
                        value={form.quantity}
                        onChange={(event) =>
                          setForm({ ...form, quantity: event.target.value })
                        }
                      />
                    </label>
                  </WorkshopForm>
                  <WorkshopForm
                    title="Reallocate stock"
                    submitLabel="Move stock"
                    onSubmit={async () => {
                      await reallocateWorkshopAmmunition({
                        sourceAllocationId: allocationId,
                        destinationFirearmId: form.firearmId,
                        quantity: Number(form.quantity),
                        idempotencyKey: crypto.randomUUID(),
                      });
                    }}
                  >
                    <label>
                      Source allocation ID
                      <input
                        required
                        value={allocationId}
                        onChange={(event) =>
                          setAllocationId(event.target.value)
                        }
                      />
                    </label>
                    <label>
                      Destination firearm
                      <select
                        required
                        value={form.firearmId}
                        onChange={(event) =>
                          setForm({ ...form, firearmId: event.target.value })
                        }
                      >
                        <option value="">Select firearm</option>
                        {firearmItems
                          .filter((firearm) => !firearm.archived)
                          .map((firearm) => (
                            <option value={firearm.id} key={firearm.id}>
                              {firearm.nickname ||
                                `${firearm.manufacturer} ${firearm.model}`}
                            </option>
                          ))}
                      </select>
                    </label>
                    <label>
                      Quantity
                      <input
                        type="number"
                        min="1"
                        required
                        value={form.quantity}
                        onChange={(event) =>
                          setForm({ ...form, quantity: event.target.value })
                        }
                      />
                    </label>
                  </WorkshopForm>
                </div>
              </>
            )}
          </ResourceState>
        )}
      </ResourceState>
    </div>
  );
}
