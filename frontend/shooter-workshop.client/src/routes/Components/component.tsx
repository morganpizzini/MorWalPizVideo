import { useState } from "react";
import { PageTitle, EmptyState } from "@morwalpiz/layout";
import type {
  WorkshopComponentDefinition,
  WorkshopPhysicalComponent,
} from "@morwalpizvideo/models";
import {
  createWorkshopComponent,
  createWorkshopComponentDefinition,
  getWorkshopComponentDefinitions,
  getWorkshopComponents,
} from "@morwalpizvideo/services";
import { WorkshopForm } from "../../components/WorkshopForm";
import { ResourceState, useResource } from "../shared";
export default function Components() {
  const definitions = useResource(getWorkshopComponentDefinitions);
  const inventory = useResource(getWorkshopComponents);
  return (
    <div>
      <PageTitle
        title="Components"
        subtitle="Catalogue definitions and independent physical inventory."
      />
      <div className="detail-grid">
        <ResourceState resource={definitions}>
          {(items) => <Catalogue items={items} />}
        </ResourceState>
        <ResourceState resource={inventory}>
          {(items) => (
            <Inventory items={items} definitions={definitions.value ?? []} />
          )}
        </ResourceState>
      </div>
    </div>
  );
}

function Catalogue({ items }: { items: WorkshopComponentDefinition[] }) {
  const [showForm, setShowForm] = useState(false);
  const [name, setName] = useState("");
  return (
    <section className="panel">
      <div className="section-heading">
        <h2>Catalogue</h2>
        <button
          className="button button-quiet"
          onClick={() => setShowForm(!showForm)}
        >
          + Definition
        </button>
      </div>
      {showForm && (
        <WorkshopForm
          title="New component definition"
          submitLabel="Save definition"
          onSubmit={async () => {
            await createWorkshopComponentDefinition({
              name,
              trackingMode: "Counter",
            });
            setShowForm(false);
          }}
        >
          <label>
            Name
            <input
              required
              value={name}
              onChange={(event) => setName(event.target.value)}
            />
          </label>
        </WorkshopForm>
      )}
      {items.length ? (
        <ul className="compact-list">
          {items.map((item) => (
            <li key={item.id}>
              <span>
                <strong>{item.name}</strong>
                <small>
                  {item.manufacturer ?? "No manufacturer"} · {item.trackingMode}{" "}
                  · interval {item.defaultServiceInterval ?? "not set"}
                </small>
              </span>
              <span className="badge">
                {item.archived ? "archived" : "active"}
              </span>
            </li>
          ))}
        </ul>
      ) : (
        <EmptyState title="Catalogue is empty" />
      )}
    </section>
  );
}

function Inventory({
  items,
  definitions,
}: {
  items: WorkshopPhysicalComponent[];
  definitions: WorkshopComponentDefinition[];
}) {
  const [definitionId, setDefinitionId] = useState("");
  const [note, setNote] = useState("");
  return (
    <section className="panel">
      <div className="section-heading">
        <h2>My inventory</h2>
      </div>
      <WorkshopForm
        title="Add physical component"
        submitLabel="Add component"
        onSubmit={async () => {
          await createWorkshopComponent({
            definitionId,
            identificationNote: note,
          });
          setDefinitionId("");
          setNote("");
        }}
      >
        <label>
          Definition
          <select
            required
            value={definitionId}
            onChange={(event) => setDefinitionId(event.target.value)}
          >
            <option value="">Select definition</option>
            {definitions.map((item) => (
              <option key={item.id} value={item.id}>
                {item.name}
              </option>
            ))}
          </select>
        </label>
        <label>
          Identification note
          <input
            value={note}
            onChange={(event) => setNote(event.target.value)}
          />
        </label>
      </WorkshopForm>
      {items.length ? (
        <ul className="compact-list">
          {items.map((item) => (
            <li key={item.id}>
              <span>
                <strong>
                  {definitions.find(
                    (definition) => definition.id === item.definitionId,
                  )?.name ?? item.definitionId}
                </strong>
                <small>
                  {item.identificationNote || "No identification note"} ·{" "}
                  {item.totalRounds.toLocaleString()} rounds
                </small>
              </span>
              <span className="status">{item.status}</span>
            </li>
          ))}
        </ul>
      ) : (
        <EmptyState title="No physical components" />
      )}
    </section>
  );
}
