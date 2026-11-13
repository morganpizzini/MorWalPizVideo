import { useEffect, useState, type ReactNode } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { PageTitle, EmptyState } from "@morwalpiz/layout";
import type {
  CreateWorkshopFirearmRequest,
  CreateWorkshopMaintenanceRequest,
  CreateWorkshopRecipeRequest,
  WorkshopActivity,
  WorkshopComponentDefinition,
  WorkshopFirearm,
  WorkshopPhysicalComponent,
} from "@morwalpizvideo/models";
import {
  allocateWorkshopAmmunition,
  createWorkshopComponent,
  createWorkshopComponentDefinition,
  createWorkshopFirearm,
  createWorkshopRecipe,
  archiveWorkshopFirearm,
  correctWorkshopShooting,
  getWorkshopBatches,
  getWorkshopDashboard,
  getWorkshopComponentDefinitions,
  getWorkshopComponents,
  getWorkshopFirearms,
  getWorkshopHistory,
  getWorkshopRecipes,
  getWorkshopReconciliation,
  recordWorkshopMaintenance,
  recordWorkshopProduction,
  recordWorkshopShooting,
  releaseWorkshopAllocation,
  reallocateWorkshopAmmunition,
  updateWorkshopFirearm,
  rebuildWorkshopProjections,
} from "@morwalpizvideo/services";
import { useWorkshopAuth } from "../auth/AuthProvider";
import { ErrorState, LoadingState } from "../components/States";
import { WorkshopForm } from "../components/WorkshopForm";

function useResource<T>(load: () => Promise<T>) {
  const [value, setValue] = useState<T | null>(null);
  const [error, setError] = useState<unknown>(null);
  const [loading, setLoading] = useState(true);
  const reload = () => {
    setLoading(true);
    setError(null);
    void load().then(setValue).catch(setError).finally(() => setLoading(false));
  };
  useEffect(reload, []);
  return { value, error, loading, reload };
}

function ResourceState<T>({
  resource,
  children,
}: {
  resource: ReturnType<typeof useResource<T>>;
  children: (value: T) => ReactNode;
}) {
  if (resource.loading) return <LoadingState />;
  if (resource.error) return <ErrorState error={resource.error} onRetry={resource.reload} />;
  return resource.value ? children(resource.value) : <EmptyState title="No data returned" message="The API returned an empty response." />;
}

function Stat({ label, value }: { label: string; value: string | number }) {
  return <div className="stat"><span>{label}</span><strong>{value}</strong></div>;
}

function ActivityList({ items }: { items: WorkshopActivity[] }) {
  return <ul className="activity-list">{items.map((item) => <li key={item.id}><span className="activity-dot" /><div><strong>{item.type}</strong><p>Operation {item.operationId}</p></div><time>{new Date(item.effectiveAt).toLocaleDateString()}</time></li>)}</ul>;
}

export function Dashboard() {
  const dashboard = useResource(getWorkshopDashboard);
  const history = useResource(getWorkshopHistory);
  if (dashboard.loading || history.loading) return <LoadingState />;
  const error = dashboard.error ?? history.error;
  if (error) return <ErrorState error={error} />;
  const overview = dashboard.value;
  if (!overview) return <ErrorState error={new Error("The dashboard response was empty.")} />;
  return <div><PageTitle title="Workshop dashboard" subtitle="Account-owned equipment, stock and operation history." /><div className="dashboard-grid"><section className="panel dashboard-hero"><p className="eyebrow">Operational overview</p><h2>Every operation remains attributable.</h2><p>Firearms, physical components, batches and shooting facts are kept independent by the API.</p><Link className="button button-primary" to="/sessions/shooting">Record a shooting session</Link></section><section className="stats-grid"><Stat label="Active firearms" value={overview.firearmCount} /><Stat label="Firearm rounds" value={overview.totalFirearmRounds.toLocaleString()} /><Stat label="Physical components" value={overview.componentCount} /><Stat label="Available stock" value={overview.availableRounds.toLocaleString()} /></section></div><section className="panel"><div className="section-heading"><h2>Quick actions</h2></div><div className="quick-actions"><Link className="quick-action" to="/firearms"><strong>Register firearm</strong><span>Keep equipment counters independent.</span></Link><Link className="quick-action" to="/recipes"><strong>Create recipe</strong><span>Capture the immutable recipe snapshot.</span></Link><Link className="quick-action" to="/production"><strong>Record production</strong><span>Create stock from a recipe.</span></Link><Link className="quick-action" to="/integrity"><strong>Check integrity</strong><span>Review reconciliation status.</span></Link></div></section><section className="panel"><div className="section-heading"><h2>Recent activity</h2><Link to="/history">View history</Link></div>{history.value?.length ? <ActivityList items={history.value.slice(0, 10)} /> : <EmptyState title="No activity yet" message="Your first operation will appear here." />}</section></div>;
}

export function Firearms() {
  const resource = useResource(getWorkshopFirearms);
  const [showForm, setShowForm] = useState(false);
  return <ResourceState resource={resource}>{(items) => <div><PageTitle title="My firearms" subtitle={`${items.length} account-owned records`} /><button className="button button-primary" onClick={() => setShowForm(!showForm)}>+ Register firearm</button>{showForm && <FirearmForm onSaved={() => { setShowForm(false); resource.reload(); }} />}{items.length ? <div className="card-grid">{items.map((firearm) => <Link className="record-card" to={`/firearms/${firearm.id}`} key={firearm.id}><div className="record-card-top"><span className="eyebrow">{firearm.caliber}</span><span className="badge">{firearm.archived ? "archived" : "active"}</span></div><h2>{firearm.nickname || `${firearm.manufacturer} ${firearm.model}`}</h2><p>{firearm.manufacturer} {firearm.model}</p><strong>{firearm.totalRounds.toLocaleString()} rounds</strong></Link>)}</div> : <EmptyState title="No firearms registered" message="Add a firearm to connect sessions, components and allocations." />}</div>}</ResourceState>;
}

function FirearmForm({ onSaved }: { onSaved: () => void }) {
  const [form, setForm] = useState<CreateWorkshopFirearmRequest>({ manufacturer: "", model: "", caliber: "", nickname: "", category: "", notes: "" });
  return <WorkshopForm title="Register firearm" submitLabel="Save firearm" onSubmit={async () => { await createWorkshopFirearm(form); onSaved(); }}><div className="form-grid">{(["manufacturer", "model", "caliber", "nickname", "category"] as const).map((field) => <label key={field}>{field}<input required={field === "manufacturer" || field === "model" || field === "caliber"} value={form[field] ?? ""} onChange={(event) => setForm({ ...form, [field]: event.target.value })} /></label>)}</div><label>Notes<textarea value={form.notes ?? ""} onChange={(event) => setForm({ ...form, notes: event.target.value })} /></label></WorkshopForm>;
}

export function FirearmDetail() {
  const { id = "" } = useParams();
  const resource = useResource(getWorkshopFirearms);
  const [editing, setEditing] = useState(false);
  return <ResourceState resource={resource}>{(items) => {
    const firearm = items.find((item) => item.id === id);
    if (!firearm) return <EmptyState title="Firearm not found" message="The record may be archived or belong to another account." />;
    return <div><PageTitle title={firearm.nickname || `${firearm.manufacturer} ${firearm.model}`} subtitle={`${firearm.caliber} · ${firearm.archived ? "archived" : "active"}`} />
      <div className="stats-grid"><Stat label="Total rounds" value={firearm.totalRounds.toLocaleString()} /><Stat label="Revision" value={firearm.revision} /><Stat label="Account record" value={firearm.id} /></div>
      {!firearm.archived && <section className="panel"><div className="section-heading"><h2>Manage firearm</h2><button className="button button-quiet" onClick={() => setEditing(!editing)}>{editing ? "Cancel" : "Edit details"}</button></div>
        {editing && <FirearmEditForm firearm={firearm} onSaved={() => { setEditing(false); resource.reload(); }} />}
        <button className="button button-quiet" onClick={async () => { await archiveWorkshopFirearm(firearm.id, { revision: firearm.revision, idempotencyKey: crypto.randomUUID() }); resource.reload(); }}>Archive firearm</button>
      </section>}
      <section className="panel"><h2>Related workflows</h2><p>Record a session against this firearm or correct a previously recorded operation.</p><Link className="button button-quiet" to="/sessions/shooting">Record session</Link></section>
    </div>;
  }}</ResourceState>;
}

function FirearmEditForm({ firearm, onSaved }: { firearm: WorkshopFirearm; onSaved: () => void }) {
  const [form, setForm] = useState<CreateWorkshopFirearmRequest>({ manufacturer: firearm.manufacturer, model: firearm.model, caliber: firearm.caliber, category: firearm.category, nickname: firearm.nickname, notes: firearm.notes });
  return <WorkshopForm title="Edit firearm" submitLabel="Save changes" onSubmit={async () => { await updateWorkshopFirearm(firearm.id, { ...form, revision: firearm.revision }); onSaved(); }}><div className="form-grid">{(["manufacturer", "model", "caliber", "nickname", "category"] as const).map((field) => <label key={field}>{field}<input required={field === "manufacturer" || field === "model" || field === "caliber"} value={form[field] ?? ""} onChange={(event) => setForm({ ...form, [field]: event.target.value })} /></label>)}</div><label>Notes<textarea value={form.notes ?? ""} onChange={(event) => setForm({ ...form, notes: event.target.value })} /></label></WorkshopForm>;
}

export function Components() {
  const definitions = useResource(getWorkshopComponentDefinitions);
  const inventory = useResource(getWorkshopComponents);
  return <div><PageTitle title="Components" subtitle="Catalogue definitions and independent physical inventory." /><div className="detail-grid"><ResourceState resource={definitions}>{(items) => <Catalogue items={items} />}</ResourceState><ResourceState resource={inventory}>{(items) => <Inventory items={items} definitions={definitions.value ?? []} />}</ResourceState></div></div>;
}

function Catalogue({ items }: { items: WorkshopComponentDefinition[] }) {
  const [showForm, setShowForm] = useState(false);
  const [name, setName] = useState("");
  return <section className="panel"><div className="section-heading"><h2>Catalogue</h2><button className="button button-quiet" onClick={() => setShowForm(!showForm)}>+ Definition</button></div>{showForm && <WorkshopForm title="New component definition" submitLabel="Save definition" onSubmit={async () => { await createWorkshopComponentDefinition({ name, trackingMode: "Counter" }); setShowForm(false); }}><label>Name<input required value={name} onChange={(event) => setName(event.target.value)} /></label></WorkshopForm>}{items.length ? <ul className="compact-list">{items.map((item) => <li key={item.id}><span><strong>{item.name}</strong><small>{item.manufacturer ?? "No manufacturer"} · {item.trackingMode} · interval {item.defaultServiceInterval ?? "not set"}</small></span><span className="badge">{item.archived ? "archived" : "active"}</span></li>)}</ul> : <EmptyState title="Catalogue is empty" />}</section>;
}

function Inventory({ items, definitions }: { items: WorkshopPhysicalComponent[]; definitions: WorkshopComponentDefinition[] }) {
  const [definitionId, setDefinitionId] = useState("");
  const [note, setNote] = useState("");
  return <section className="panel"><div className="section-heading"><h2>My inventory</h2></div><WorkshopForm title="Add physical component" submitLabel="Add component" onSubmit={async () => { await createWorkshopComponent({ definitionId, identificationNote: note }); setDefinitionId(""); setNote(""); }}><label>Definition<select required value={definitionId} onChange={(event) => setDefinitionId(event.target.value)}><option value="">Select definition</option>{definitions.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label><label>Identification note<input value={note} onChange={(event) => setNote(event.target.value)} /></label></WorkshopForm>{items.length ? <ul className="compact-list">{items.map((item) => <li key={item.id}><span><strong>{definitions.find((definition) => definition.id === item.definitionId)?.name ?? item.definitionId}</strong><small>{item.identificationNote || "No identification note"} · {item.totalRounds.toLocaleString()} rounds</small></span><span className="status">{item.status}</span></li>)}</ul> : <EmptyState title="No physical components" />}</section>;
}

export function Maintenance() {
  const inventory = useResource(getWorkshopComponents);
  const [form, setForm] = useState<CreateWorkshopMaintenanceRequest>({
    componentId: null,
    eventType: "Inspection",
    date: new Date().toISOString().slice(0, 10),
    description: "",
    roundCountAtEvent: null,
    idempotencyKey: "",
  });
  return <ResourceState resource={inventory}>{(items) => <div><PageTitle title="Maintenance" subtitle="Component projections and service intervals." /><section className="panel"><WorkshopForm title="Record maintenance" submitLabel="Save event" onSubmit={async () => { await recordWorkshopMaintenance({ ...form, idempotencyKey: crypto.randomUUID() }); setForm({ ...form, description: "" }); }}><label>Component<select required value={form.componentId ?? ""} onChange={(event) => setForm({ ...form, componentId: event.target.value || null })}><option value="">Select component</option>{items.map((item) => <option value={item.id} key={item.id}>{item.id} · {item.status}</option>)}</select></label><div className="form-grid"><label>Event type<select value={form.eventType} onChange={(event) => setForm({ ...form, eventType: event.target.value as CreateWorkshopMaintenanceRequest["eventType"] })}><option value="Inspection">Inspection</option><option value="Cleaning">Cleaning</option><option value="ComponentMaintenance">Component maintenance</option><option value="Replacement">Replacement</option><option value="General">General</option></select></label><label>Date<input type="date" required value={form.date} onChange={(event) => setForm({ ...form, date: event.target.value })} /></label></div><label>Round count<input type="number" min="0" value={form.roundCountAtEvent ?? ""} onChange={(event) => setForm({ ...form, roundCountAtEvent: event.target.value ? Number(event.target.value) : null })} /></label><label>Description<textarea required value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} /></label></WorkshopForm>{items.length ? <ul className="compact-list">{items.map((item) => <li key={item.id}><span><strong>{item.id}</strong><small>{item.totalRounds.toLocaleString()} rounds · interval {item.serviceInterval ?? "manual"}</small></span><span className="badge">{item.status}</span></li>)}</ul> : <EmptyState title="No tracked components" />}</section></div>}</ResourceState>;
}

export function Recipes() {
  const resource = useResource(getWorkshopRecipes);
  const [showForm, setShowForm] = useState(false);
  return <ResourceState resource={resource}>{(items) => <div><PageTitle title="Ammunition recipes" subtitle="Recipe fields match the server snapshot contract." /><button className="button button-primary" onClick={() => setShowForm(!showForm)}>+ New recipe</button>{showForm && <RecipeForm onSaved={() => { setShowForm(false); resource.reload(); }} />}{items.length ? <div className="card-grid">{items.map((recipe) => <article className="record-card" key={recipe.id}><div className="record-card-top"><span className="eyebrow">{recipe.caliber}</span><span className="badge">v{recipe.version}</span></div><h2>{recipe.name}</h2><p>{recipe.powder ?? "Powder not set"} · {recipe.status}</p></article>)}</div> : <EmptyState title="No recipes yet" />}</div>}</ResourceState>;
}

function RecipeForm({ onSaved }: { onSaved: () => void }) {
  const [form, setForm] = useState<CreateWorkshopRecipeRequest>({ name: "", caliber: "", oal: "", bulletWeight: null, bulletForm: "", drawing: "", primer: "", powder: "", powderWeight: null, notes: "" });
  return <WorkshopForm title="New recipe" submitLabel="Save recipe" onSubmit={async () => { await createWorkshopRecipe(form); onSaved(); }}><div className="form-grid"><label>Name<input required value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} /></label><label>Caliber<input required value={form.caliber} onChange={(event) => setForm({ ...form, caliber: event.target.value })} /></label><label>Powder<input value={form.powder ?? ""} onChange={(event) => setForm({ ...form, powder: event.target.value })} /></label><label>Powder weight<input type="number" value={form.powderWeight ?? ""} onChange={(event) => setForm({ ...form, powderWeight: event.target.value ? Number(event.target.value) : null })} /></label></div><label>Notes<textarea value={form.notes ?? ""} onChange={(event) => setForm({ ...form, notes: event.target.value })} /></label></WorkshopForm>;
}

export function Ammunition() {
  const resource = useResource(getWorkshopBatches);
  return <ResourceState resource={resource}>{(items) => <div><PageTitle title="Ammunition" subtitle="Produced batches and current on-hand stock." />{items.length ? <div className="table-wrap"><table><thead><tr><th>Batch</th><th>Caliber</th><th>Produced</th><th>On hand</th><th>Revision</th></tr></thead><tbody>{items.map((batch) => <tr key={batch.id}><td>{batch.id}</td><td>{batch.recipeSnapshot.caliber}</td><td>{batch.producedQuantity}</td><td><strong>{batch.onHandQuantity}</strong></td><td>{batch.revision}</td></tr>)}</tbody></table></div> : <EmptyState title="No ammunition batches" />}</div>}</ResourceState>;
}

export function Production() {
  const recipes = useResource(getWorkshopRecipes);
  const [form, setForm] = useState({ recipeId: "", quantityProduced: "0", productionDate: new Date().toISOString().slice(0, 10), notes: "" });
  return <div><PageTitle title="Production sessions" subtitle="POST /api/v1/workshop/ammunition/production creates a batch." /><ResourceState resource={recipes}>{(items) => <WorkshopForm title="Record production" submitLabel="Create batch" onSubmit={async () => { await recordWorkshopProduction({ recipeId: form.recipeId, productionDate: form.productionDate, quantityProduced: Number(form.quantityProduced), notes: form.notes, idempotencyKey: crypto.randomUUID() }); }}><label>Recipe<select required value={form.recipeId} onChange={(event) => setForm({ ...form, recipeId: event.target.value })}><option value="">Select recipe</option>{items.map((recipe) => <option value={recipe.id} key={recipe.id}>{recipe.name} · {recipe.caliber}</option>)}</select></label><div className="form-grid"><label>Quantity<input type="number" min="1" required value={form.quantityProduced} onChange={(event) => setForm({ ...form, quantityProduced: event.target.value })} /></label><label>Date<input type="date" required value={form.productionDate} onChange={(event) => setForm({ ...form, productionDate: event.target.value })} /></label></div><label>Notes<textarea value={form.notes} onChange={(event) => setForm({ ...form, notes: event.target.value })} /></label></WorkshopForm>}</ResourceState></div>;
}

export function ShootingSessions() {
  const firearms = useResource(getWorkshopFirearms);
  const [form, setForm] = useState({ firearmId: "", rounds: "0", untracked: "0", consumption: "[]", usage: "[]" });
  return <div><PageTitle title="Shooting sessions" subtitle="Record new sessions or correct an operation without rewriting history." /><ResourceState resource={firearms}>{(items) => <><WorkshopForm title="Record shooting" submitLabel="Record session" onSubmit={async () => { const totalRounds = Number(form.rounds); await recordWorkshopShooting({ firearmId: form.firearmId, date: new Date().toISOString(), totalRounds, untrackedAmmunition: Number(form.untracked), consumptionRecords: JSON.parse(form.consumption), usageSegments: JSON.parse(form.usage), idempotencyKey: crypto.randomUUID() }); }}><label>Firearm<select required value={form.firearmId} onChange={(event) => setForm({ ...form, firearmId: event.target.value })}><option value="">Select firearm</option>{items.filter((firearm) => !firearm.archived).map((firearm) => <option value={firearm.id} key={firearm.id}>{firearm.nickname || `${firearm.manufacturer} ${firearm.model}`}</option>)}</select></label><div className="form-grid"><label>Total rounds<input type="number" min="1" required value={form.rounds} onChange={(event) => setForm({ ...form, rounds: event.target.value })} /></label><label>Untracked ammunition<input type="number" min="0" required value={form.untracked} onChange={(event) => setForm({ ...form, untracked: event.target.value })} /></label></div><label>Consumption records JSON<textarea value={form.consumption} onChange={(event) => setForm({ ...form, consumption: event.target.value })} /></label><label>Usage segments JSON<textarea value={form.usage} onChange={(event) => setForm({ ...form, usage: event.target.value })} /></label></WorkshopForm><ShootingCorrectionForm firearms={items.filter((firearm) => !firearm.archived)} /></>}</ResourceState></div>;
}

function ShootingCorrectionForm({ firearms }: { firearms: WorkshopFirearm[] }) {
  const [operationId, setOperationId] = useState("");
  const [firearmId, setFirearmId] = useState("");
  const [rounds, setRounds] = useState("0");
  return <WorkshopForm title="Correct shooting operation" submitLabel="Record correction" onSubmit={async () => { await correctWorkshopShooting({ originalOperationId: operationId, replacementShooting: { firearmId, date: new Date().toISOString(), totalRounds: Number(rounds), untrackedAmmunition: Number(rounds), consumptionRecords: [], usageSegments: [], idempotencyKey: crypto.randomUUID() }, idempotencyKey: crypto.randomUUID() }); }}><label>Original operation ID<input required value={operationId} onChange={(event) => setOperationId(event.target.value)} /></label><div className="form-grid"><label>Correct firearm<select required value={firearmId} onChange={(event) => setFirearmId(event.target.value)}><option value="">Select firearm</option>{firearms.map((firearm) => <option value={firearm.id} key={firearm.id}>{firearm.nickname || `${firearm.manufacturer} ${firearm.model}`}</option>)}</select></label><label>Correct total rounds<input type="number" min="1" required value={rounds} onChange={(event) => setRounds(event.target.value)} /></label></div><small>A correction appends a compensating operation; the original remains in history.</small></WorkshopForm>;
}

export function Allocation() {
  const batches = useResource(getWorkshopBatches);
  const firearms = useResource(getWorkshopFirearms);
  const [form, setForm] = useState({ batchId: "", firearmId: "", quantity: "0", notes: "" });
  const [allocationId, setAllocationId] = useState("");
  const [revision, setRevision] = useState("0");
  return <div><PageTitle title="Ammunition allocation" subtitle="Allocate, release, or move stock between firearms." /><ResourceState resource={batches}>{(batchItems) => <ResourceState resource={firearms}>{(firearmItems) => <><WorkshopForm title="Allocate stock" submitLabel="Allocate" onSubmit={async () => { await allocateWorkshopAmmunition({ batchId: form.batchId, firearmId: form.firearmId, quantity: Number(form.quantity), notes: form.notes, idempotencyKey: crypto.randomUUID() }); }}><label>Batch<select required value={form.batchId} onChange={(event) => setForm({ ...form, batchId: event.target.value })}><option value="">Select batch</option>{batchItems.map((batch) => <option value={batch.id} key={batch.id}>{batch.id} · {batch.onHandQuantity} on hand</option>)}</select></label><label>Firearm<select required value={form.firearmId} onChange={(event) => setForm({ ...form, firearmId: event.target.value })}><option value="">Select firearm</option>{firearmItems.filter((firearm) => !firearm.archived).map((firearm) => <option value={firearm.id} key={firearm.id}>{firearm.nickname || `${firearm.manufacturer} ${firearm.model}`}</option>)}</select></label><label>Quantity<input type="number" min="1" required value={form.quantity} onChange={(event) => setForm({ ...form, quantity: event.target.value })} /></label><label>Notes<textarea value={form.notes} onChange={(event) => setForm({ ...form, notes: event.target.value })} /></label></WorkshopForm><div className="detail-grid"><WorkshopForm title="Release allocation" submitLabel="Release stock" onSubmit={async () => { await releaseWorkshopAllocation(allocationId, { quantity: Number(form.quantity), revision: Number(revision), idempotencyKey: crypto.randomUUID() }); }}><label>Allocation ID<input required value={allocationId} onChange={(event) => setAllocationId(event.target.value)} /></label><label>Current revision<input type="number" min="0" required value={revision} onChange={(event) => setRevision(event.target.value)} /></label><label>Quantity<input type="number" min="1" required value={form.quantity} onChange={(event) => setForm({ ...form, quantity: event.target.value })} /></label></WorkshopForm><WorkshopForm title="Reallocate stock" submitLabel="Move stock" onSubmit={async () => { await reallocateWorkshopAmmunition({ sourceAllocationId: allocationId, destinationFirearmId: form.firearmId, quantity: Number(form.quantity), idempotencyKey: crypto.randomUUID() }); }}><label>Source allocation ID<input required value={allocationId} onChange={(event) => setAllocationId(event.target.value)} /></label><label>Destination firearm<select required value={form.firearmId} onChange={(event) => setForm({ ...form, firearmId: event.target.value })}><option value="">Select firearm</option>{firearmItems.filter((firearm) => !firearm.archived).map((firearm) => <option value={firearm.id} key={firearm.id}>{firearm.nickname || `${firearm.manufacturer} ${firearm.model}`}</option>)}</select></label><label>Quantity<input type="number" min="1" required value={form.quantity} onChange={(event) => setForm({ ...form, quantity: event.target.value })} /></label></WorkshopForm></div></>}</ResourceState>}</ResourceState></div>;
}

export function History() {
  const resource = useResource(getWorkshopHistory);
  return <ResourceState resource={resource}>{(items) => <div><PageTitle title="History" subtitle="Account-filtered chronological activity from the controller." />{items.length ? <section className="panel"><ActivityList items={items} /></section> : <EmptyState title="No history yet" />}</div>}</ResourceState>;
}

export function Reconciliation() {
  const resource = useResource(getWorkshopReconciliation);
  return <ResourceState resource={resource}>{(status) => <div><PageTitle title="Integrity and reconciliation" subtitle="Projection state is returned by the canonical reconciliation endpoint." /><section className={`panel projection projection-${status.state.toLowerCase()}`}><h2>{status.state}</h2><p>{status.mismatchCount ? `${status.mismatchCount} mismatches detected.` : "No mismatches reported."}</p><p>Last run: {new Date(status.lastRunAt).toLocaleString()}</p>{status.mismatches.length > 0 && <ul>{status.mismatches.map((mismatch) => <li key={mismatch}>{mismatch}</li>)}</ul>}<button className="button button-primary" onClick={() => void rebuildWorkshopProjections().then(resource.reload)}>Rebuild projections</button></section></div>}</ResourceState>;
}

export function AccountLogin() {
  const navigate = useNavigate();
  const [identifier, setIdentifier] = useState("");
  const [password, setPassword] = useState("");
  const [message, setMessage] = useState("");
  const { login } = useWorkshopAuth();
  return <main className="account-page"><div className="account-card"><p className="eyebrow">Private workspace</p><h1>Sign in to Workshop</h1><p>Use your existing MorWalPiz account. Workshop requests stay on the account cookie transport.</p><form onSubmit={(event) => { event.preventDefault(); void login({ identifier, password }).then((ok) => { if (ok) void navigate("/"); else setMessage("Sign-in was not accepted."); }); }}><label>Username or email<input required value={identifier} onChange={(event) => setIdentifier(event.target.value)} /></label><label>Password<input required type="password" value={password} onChange={(event) => setPassword(event.target.value)} /></label>{message && <p className="form-error" role="alert">{message}</p>}<button className="button button-primary">Sign in</button></form></div></main>;
}

export function AccountProfile() {
  const { session } = useWorkshopAuth();
  return <div><PageTitle title="Account" subtitle="Shared MorWalPiz account identity" /><section className="panel"><dl className="definition-list"><dt>Username</dt><dd>{session.account?.username}</dd><dt>Email</dt><dd>{session.account?.email}</dd><dt>Display name</dt><dd>{session.account?.displayName}</dd></dl></section></div>;
}

export function ComponentsUnavailable() {
  return <EmptyState title="Unavailable route" message="Use the Components workspace for catalogue and inventory." />;
}
