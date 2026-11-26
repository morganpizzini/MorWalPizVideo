import { useEffect, useMemo, useRef, useState } from "react";
import { useFetcher } from "react-router";
import { EmptyState } from "@morwalpiz/layout";
import type { WorkshopComponentDefinition, WorkshopPhysicalComponent, WorkshopTrackingMode } from "@morwalpizvideo/models";
import { useI18n } from "../../i18n";
import { useWorkshopAuth } from "../../auth/AuthProvider";
import { formatNumber, localDateInputValue } from "../../i18n-formatters";
import type { action } from "./action";
import type { EquipmentLoaderData } from "./loader";

type ActionFetcher = ReturnType<typeof useFetcher<typeof action>>;
const UNINSTALLED = "__uninstalled";

export function EquipmentPanels({ definitions, inventory, firearms, installations, firearmId, onFirearmChange }: EquipmentLoaderData & {
  firearmId: string;
  onFirearmChange: (value: string) => void;
}) {
  return (
    <Inventory {...{ inventory, definitions, firearms, installations, firearmId, onFirearmChange }} />
  );
}

function Modal({ title, children, onClose }: { title: string; children: React.ReactNode; onClose: () => void }) {
  const dialogRef = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const previous = document.activeElement as HTMLElement | null;
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") onClose();
      if (event.key !== "Tab" || !dialogRef.current) return;
      const focusable = [...dialogRef.current.querySelectorAll<HTMLElement>("button, input, select, textarea")].filter((element) => !element.hasAttribute("disabled"));
      if (!focusable.length) return;
      const current = document.activeElement;
      const index = focusable.indexOf(current as HTMLElement);
      if (event.shiftKey && (index <= 0)) { event.preventDefault(); focusable.at(-1)?.focus(); }
      else if (!event.shiftKey && index === focusable.length - 1) { event.preventDefault(); focusable[0]?.focus(); }
    };
    document.addEventListener("keydown", onKeyDown);
    dialogRef.current?.querySelector<HTMLElement>("input, select, textarea, button")?.focus();
    return () => { document.removeEventListener("keydown", onKeyDown); previous?.focus(); };
  }, [onClose]);
  return (
    <div className="feedback-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) onClose(); }}>
      <div ref={dialogRef} className="feedback-dialog component-dialog" role="dialog" aria-modal="true" aria-labelledby="component-dialog-title">
        <div className="section-heading"><h2 id="component-dialog-title">{title}</h2><button className="button button-quiet" type="button" onClick={onClose}>×</button></div>
        {children}
      </div>
    </div>
  );
}

function ActionError({ fetcher }: { fetcher: ActionFetcher }) {
  const { t } = useI18n();
  if (fetcher.state !== "idle" || !fetcher.data || fetcher.data.ok) return null;
  const message = typeof fetcher.data === "object" && fetcher.data && "error" in fetcher.data ? fetcher.data.error : "The request could not be completed.";
  const details = typeof fetcher.data === "object" && fetcher.data && "details" in fetcher.data ? fetcher.data.details : null;
  return <p className="form-error" role="alert">{t(String(message))}{details ? ` ${details}` : ""}</p>;
}

function SearchBox({ value, onChange, label }: { value: string; onChange: (value: string) => void; label: string }) {
  return <label className="search-field">{label}<input type="search" value={value} onChange={(event) => onChange(event.target.value)} /></label>;
}

export function Catalogue({ items, offline = false }: { items: WorkshopComponentDefinition[]; offline?: boolean }) {
  const { t } = useI18n();
  const [query, setQuery] = useState("");
  const [modal, setModal] = useState<{ item?: WorkshopComponentDefinition } | null>(null);
  const fetcher = useFetcher<typeof action>();
  const visible = useMemo(() => {
    const normalized = query.trim().toLocaleLowerCase();
    return normalized ? items.filter((item) => [item.name, item.description, item.notes].join(" ").toLocaleLowerCase().includes(normalized)) : items;
  }, [items, query]);
  useEffect(() => { if (fetcher.state === "idle" && fetcher.data?.ok) setModal(null); }, [fetcher.state, fetcher.data]);
  return (
    <section className="panel" id="component-panel-catalogue">
      <div className="section-heading"><div><h2>{t("Catalogue")}</h2><small>{visible.length} / {items.length}</small></div><button className="button button-primary" type="button" onClick={() => setModal({})}>+ {t("New component definition")}</button></div>
      <SearchBox value={query} onChange={setQuery} label={t("Search catalogue")} />
      {visible.length ? <div className="table-wrap mt-3"><table><thead><tr><th>{t("Name")}</th><th>{t("Description")}</th><th>{t("Status")}</th><th>{t("Actions")}</th></tr></thead><tbody>{visible.map((item) => <tr key={item.id}><td><strong>{item.name}</strong></td><td>{item.description || t("No description")}</td><td><span className="badge">{t(item.archived ? "archived" : "active")}</span></td><td className="record-actions">{!offline && <button className="button button-quiet" type="button" onClick={() => setModal({ item })}>{t("Edit")}</button>}{!item.archived && <ManagementAction fetcher={fetcher} intent="archive-definition" item={item} label={t("Archive")} />}{!offline && <ManagementAction fetcher={fetcher} intent="delete-definition" item={item} label={t("Delete")} destructive />}</td></tr>)}</tbody></table></div> : <EmptyState title={items.length ? t("No catalogue results") : t("Catalogue is empty")} />}
      {modal && <Modal title={t(modal.item ? "Edit component definition" : "New component definition")} onClose={() => setModal(null)}><DefinitionForm fetcher={fetcher} item={modal.item} /></Modal>}
    </section>
  );
}

function DefinitionForm({ fetcher, item }: { fetcher: ActionFetcher; item?: WorkshopComponentDefinition }) {
  const { t } = useI18n();
  return <fetcher.Form method="post" className="form-panel"><input type="hidden" name="intent" value={item ? "update-definition" : "create-definition"} />{item && <><input type="hidden" name="id" value={item.id} /><input type="hidden" name="revision" value={item.revision} /></>}<label>{t("Name")}<input name="name" defaultValue={item?.name ?? ""} required /></label><label>{t("Description")}<textarea name="description" defaultValue={item?.description ?? ""} /></label><label>{t("Notes")}<textarea name="notes" defaultValue={item?.notes ?? ""} /></label><ActionError fetcher={fetcher} /><button className="button button-primary" disabled={fetcher.state !== "idle"}>{t(item ? "Save" : "Save definition")}</button></fetcher.Form>;
}

function ManagementAction({ fetcher, intent, item, label, destructive = false }: { fetcher: ActionFetcher; intent: string; item: { id: string; revision: number }; label: string; destructive?: boolean }) {
  const { t } = useI18n();
  return <fetcher.Form method="post" onSubmit={(event) => { if (destructive && !window.confirm(t("This record has no removable history. Delete it? Archive is safer when it is in use."))) event.preventDefault(); }}><input type="hidden" name="intent" value={intent} /><input type="hidden" name="id" value={item.id} /><input type="hidden" name="revision" value={item.revision} /><button className="button button-quiet" type="submit" disabled={fetcher.state !== "idle"}>{label}</button></fetcher.Form>;
}

export function Inventory({ inventory, definitions, firearms, installations, firearmId, onFirearmChange }: { inventory: WorkshopPhysicalComponent[]; definitions: WorkshopComponentDefinition[]; firearms: EquipmentLoaderData["firearms"]; installations: EquipmentLoaderData["installations"]; firearmId: string; onFirearmChange: (value: string) => void }) {
  const { culture, t } = useI18n();
  const { offline } = useWorkshopAuth();
  const [query, setQuery] = useState("");
  const [selectedTags, setSelectedTags] = useState<string[]>([]);
  const [modal, setModal] = useState<{ item?: WorkshopPhysicalComponent } | null>(null);
  const fetcher = useFetcher<typeof action>();
  const definitionById = useMemo(() => new Map(definitions.map((item) => [item.id, item])), [definitions]);
  const firearmById = useMemo(() => new Map(firearms.map((item) => [item.id, item])), [firearms]);
  const installationByComponent = useMemo(() => new Map(installations.filter((item) => !item.removedAt).map((item) => [item.componentId, item])), [installations]);
  const tags = useMemo(() => [...new Set(inventory.flatMap((item) => item.compatibilityTags))].sort(), [inventory]);
  const visible = useMemo(() => {
    const normalized = query.trim().toLocaleLowerCase();
    return inventory.filter((item) => {
      const installation = installationByComponent.get(item.id);
      if (firearmId === UNINSTALLED ? installation : firearmId && installation?.firearmId !== firearmId) return false;
      if (selectedTags.length && !selectedTags.every((tag) => item.compatibilityTags.includes(tag))) return false;
      if (!normalized) return true;
      const definition = definitionById.get(item.definitionId);
      const firearm = installation ? firearmById.get(installation.firearmId) : undefined;
      return [definition?.name, definition?.description, definition?.notes, item.identificationNote, item.manufacturer, item.model, item.compatibilityTags.join(" "), firearm?.nickname, firearm?.manufacturer, firearm?.model, item.status].join(" ").toLocaleLowerCase().includes(normalized);
    });
  }, [inventory, firearmId, selectedTags, query, definitionById, firearmById, installationByComponent]);
  useEffect(() => { if (fetcher.state === "idle" && fetcher.data?.ok) setModal(null); }, [fetcher.state, fetcher.data]);
  return (
    <section className="panel" id="component-panel-inventory">
      <div className="section-heading"><div><h2>{t("My inventory")}</h2><small>{visible.length} / {inventory.length}</small></div><button className="button button-primary" type="button" onClick={() => setModal({})}>+ {t("Add physical component")}</button></div>
      <div className="component-filters"><SearchBox value={query} onChange={setQuery} label={t("Search inventory")} /><label>{t("Filter by firearm")}<select value={firearmId} onChange={(event) => onFirearmChange(event.target.value)}><option value="">{t("All firearms")}</option><option value={UNINSTALLED}>{t("Uninstalled")}</option>{firearms.filter((firearm) => !firearm.archived).map((firearm) => <option key={firearm.id} value={firearm.id}>{firearm.nickname || `${firearm.manufacturer} ${firearm.model}`}</option>)}</select></label></div>
      <div className="filter-row"><span className="filter-label">{t("Filter by compatibility tag")}</span><div className="tag-list">{tags.map((tag) => <button className={`tag-pill${selectedTags.includes(tag) ? " tag-pill-active" : ""}`} type="button" aria-pressed={selectedTags.includes(tag)} key={tag} onClick={() => setSelectedTags((current) => current.includes(tag) ? current.filter((value) => value !== tag) : [...current, tag])}>{tag}</button>)}</div>{selectedTags.length > 0 && <button className="button button-quiet" type="button" onClick={() => setSelectedTags([])}>{t("Clear")}</button>}</div>
      <ActionError fetcher={fetcher} />
      {visible.length ? <div className="table-wrap"><table><thead><tr><th>{t("Component")}</th><th>{t("Details")}</th><th>{t("Rounds")}</th><th>{t("Installed on")}</th><th>{t("Actions")}</th></tr></thead><tbody>{visible.map((item) => { const installation = installationByComponent.get(item.id); const firearm = installation ? firearmById.get(installation.firearmId) : undefined; return <tr key={item.id}><td><strong>{definitionById.get(item.definitionId)?.name ?? item.definitionId}</strong>&nbsp;<small>{item.identificationNote || ""}</small></td><td>{item.manufacturer || t("No manufacturer")} · {item.model || t("No model")}&nbsp;<small>{item.compatibilityTags.join(", ") || ""}</small></td><td>{formatNumber(item.totalRounds, culture)}</td><td>{firearm ? firearm.nickname || `${firearm.manufacturer} ${firearm.model}` : t("Uninstalled")}</td><td className="record-actions">{!offline && <><button className="button button-quiet" type="button" onClick={() => setModal({ item })}>{t("Edit")}</button><ManagementAction fetcher={fetcher} intent="archive-component" item={item} label={t("Archive")} /><ManagementAction fetcher={fetcher} intent="delete-component" item={item} label={t("Delete")} destructive /></>}</td></tr>; })}</tbody></table></div> : <EmptyState title={inventory.length ? t("No inventory results") : t("No physical components")} />}
      {modal && <Modal title={t(modal.item ? "Edit physical component" : "Add physical component")} onClose={() => setModal(null)}><ComponentForm fetcher={fetcher} definitions={definitions} inventory={inventory} firearms={firearms} installations={installations} item={modal.item} installation={modal.item ? installationByComponent.get(modal.item.id) : undefined} initialFirearmId={firearmId === UNINSTALLED ? "" : firearmId} offline={offline} /></Modal>}
    </section>
  );
}

function ComponentForm({ fetcher, definitions, inventory, firearms, installations, item, installation, initialFirearmId = "", offline }: { fetcher: ActionFetcher; definitions: WorkshopComponentDefinition[]; inventory: WorkshopPhysicalComponent[]; firearms: EquipmentLoaderData["firearms"]; installations: EquipmentLoaderData["installations"]; item?: WorkshopPhysicalComponent; installation?: EquipmentLoaderData["installations"][number]; initialFirearmId?: string; offline: boolean }) {
  const { t } = useI18n();
  const [trackingMode, setTrackingMode] = useState<WorkshopTrackingMode>(item?.trackingMode ?? "Counter");
  const [firearmId, setFirearmId] = useState(installation?.firearmId ?? initialFirearmId);
  const [replacementInstallationId, setReplacementInstallationId] = useState("");
  const [tags, setTags] = useState(item?.compatibilityTags ?? []);
  const [tagInput, setTagInput] = useState("");
  const [quantity, setQuantity] = useState(1);
  const [creationKey] = useState(() => crypto.randomUUID());
  const addTag = () => { const tag = tagInput.trim(); if (tag && !tags.includes(tag)) setTags((current) => [...current, tag]); setTagInput(""); };
  const replaceable = installations.filter(entry => !entry.removedAt && entry.firearmId === firearmId && entry.componentId !== item?.id);
  const componentById = new Map(inventory.map(component => [component.id, component]));
  const definitionById = new Map(definitions.map(definition => [definition.id, definition]));
  const replacement = replaceable.find(entry => entry.id === replacementInstallationId);
  const multiple = !item && quantity > 1;
  const displayedQuantity = Number.isSafeInteger(quantity) && quantity > 0 ? quantity : 1;
  return (
    <fetcher.Form method="post" className="form-panel">
      <input type="hidden" name="intent" value={item ? "update-component" : "create-component"} />
      {!item && <input type="hidden" name="creationIdempotencyKey" value={creationKey} />}
      {item && <>
        <input type="hidden" name="id" value={item.id} />
        <input type="hidden" name="revision" value={item.revision} />
        <input type="hidden" name="installationId" value={installation?.id ?? ""} />
        <input type="hidden" name="installationRevision" value={installation?.revision ?? 0} />
        <input type="hidden" name="existingFirearmId" value={installation?.firearmId ?? ""} />
      </>}
      {replacement && !multiple && <>
        <input type="hidden" name="replacementInstallationId" value={replacement.id} />
        <input type="hidden" name="replacementInstallationRevision" value={replacement.revision} />
        <input type="hidden" name="replacementInstallation" value={JSON.stringify(replacement)} />
      </>}
      {!item && (
        <label>
          {t("Individual components")}
          <input
            type="number"
            name="componentQuantity"
            min="1"
            max="100"
            step="1"
            required
            value={quantity}
            onChange={(event) => {
              const next = Number(event.target.value);
              setQuantity(Number.isFinite(next) ? next : 1);
            }}
          />
        </label>
      )}
      <label>{t("Definition")}<select name="definitionId" defaultValue={item?.definitionId ?? ""} required><option value="">{t("Select definition")}</option>{definitions.filter((definition) => !definition.archived).map((definition) => <option key={definition.id} value={definition.id}>{definition.name}</option>)}</select></label>
      <div className="form-grid">
        <label>{t("Manufacturer")}<input name="manufacturer" defaultValue={item?.manufacturer ?? ""} /></label>
        <label>{t("Model")}<input name="model" defaultValue={item?.model ?? ""} /></label>
      </div>
      <label>{t(item ? "Current round count" : "Initial round count")}<input type="number" min="0" step="1" name={item ? "totalRounds" : "initialRounds"} defaultValue={item?.totalRounds ?? 0} required /></label>
      <label>{t("Compatibility tags")}<input value={tagInput} onChange={(event) => { const value = event.target.value; if (value.includes(",")) { const parts = value.split(","); setTagInput(parts.pop() ?? ""); setTags((current) => [...current, ...parts.map((part) => part.trim()).filter((part) => part && !current.includes(part))]); } else setTagInput(value); }} onKeyDown={(event) => { if (event.key === "," || event.key === "Enter") { event.preventDefault(); addTag(); } }} placeholder={t("Type a tag and press comma")} /><input type="hidden" name="compatibilityTags" value={tags.join(",")} /><span className="tag-list">{tags.map((tag) => <button className="tag-pill tag-pill-active" type="button" key={tag} onClick={() => setTags((current) => current.filter((value) => value !== tag))}>{tag} ×</button>)}</span></label>
      <label>{t("Tracking mode")}<select name="trackingMode" value={trackingMode} onChange={(event) => setTrackingMode(event.target.value as WorkshopTrackingMode)}><option value="Counter">{t("Counter")}</option><option value="Interval">{t("Interval")}</option><option value="Manual">{t("Manual")}</option></select></label>
      {trackingMode === "Interval" && <label>{t("Maximum rounds")}<input type="number" min="1" name="serviceInterval" defaultValue={item?.serviceInterval ?? ""} required /></label>}
      <label>{t("Firearm (optional)")}<select name="firearmId" value={firearmId} disabled={multiple} onChange={(event) => { setFirearmId(event.target.value); setReplacementInstallationId(""); }}><option value="">{t("Leave empty to add a spare part")}</option>{firearms.filter((firearm) => !firearm.archived).map((firearm) => <option key={firearm.id} value={firearm.id}>{firearm.nickname || `${firearm.manufacturer} ${firearm.model}`}</option>)}</select></label>
      {multiple && <p className="status-note">{t("Bulk creation adds uninstalled spare components. Create one component to install or replace it.")}</p>}
      {!item && !multiple && firearmId && replaceable.length > 0 && <label>{t("Replace installed component (optional)")}<select value={replacementInstallationId} onChange={event => setReplacementInstallationId(event.target.value)}><option value="">{t("Do not replace an installed component")}</option>{replaceable.map(entry => { const existing = componentById.get(entry.componentId); return <option key={entry.id} value={entry.id}>{definitionById.get(existing?.definitionId ?? "")?.name ?? t("Unknown component")}</option>; })}</select><small>{t("The current component is removed before the new one is installed.")}</small></label>}
      {!item && (
        <fieldset className="component-purchase-fields">
          <legend>{t("Purchase price (optional)")}</legend>
          <p>{t("Price is per physical component.")}</p>
          <div className="form-grid">
            <label>{t("Purchase date")}<input type="date" name="purchaseDate" defaultValue={localDateInputValue()} /></label>
            <label>{t("Price per component")}<input type="number" name="unitPrice" min="0" step="0.01" /></label>
            <label>{t("Currency (ISO code)")}<input name="currency" maxLength={3} minLength={3} pattern="[A-Za-z]{3}" defaultValue="EUR" /></label>
          </div>
          {offline && <small>{t("Bulk creation and purchase prices require an online connection. One component can still be added offline.")}</small>}
        </fieldset>
      )}
      <label>{t("Identification note")}<small>{t("Short label used to identify this physical part.")}</small><input name="identificationNote" defaultValue={item?.identificationNote ?? ""} /></label>
      <label>{t("Notes")}<small>{t("Additional maintenance or workshop context.")}</small><textarea name="notes" defaultValue={item?.notes ?? ""} /></label>
      <ActionError fetcher={fetcher} />
      <button className="button button-primary" disabled={fetcher.state !== "idle"}>
        {t(item ? "Save" : displayedQuantity === 1 ? "Add component" : "Add {count} components", { count: displayedQuantity })}
      </button>
    </fetcher.Form>
  );
}
