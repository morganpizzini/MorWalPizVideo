import { useEffect, useMemo, useRef, useState } from "react";
import { useFetcher, useLoaderData, useSearchParams } from "react-router";
import { EmptyState, PageTitle } from "@morwalpiz/layout";
import type { WorkshopComponentDefinition, WorkshopPhysicalComponent, WorkshopTrackingMode } from "@morwalpizvideo/models";
import { useI18n } from "../../i18n";
import { formatNumber } from "../../i18n-formatters";
import type { action } from "./action";
import type { ComponentsLoaderData } from "./loader";

type ActionFetcher = ReturnType<typeof useFetcher<typeof action>>;
const UNINSTALLED = "__uninstalled";

export default function Components() {
  const data = useLoaderData() as ComponentsLoaderData;
  const [searchParams, setSearchParams] = useSearchParams();
  const firearmId = searchParams.get("firearmId") ?? "";
  const validFirearm = firearmId === UNINSTALLED || data.firearms.some((firearm) => firearm.id === firearmId);
  useEffect(() => {
    if (firearmId && !validFirearm) {
      const next = new URLSearchParams(searchParams);
      next.delete("firearmId");
      setSearchParams(next, { replace: true });
    }
  }, [firearmId, validFirearm, searchParams, setSearchParams]);

  return (
    <div>
      <PageTitle title="Components" subtitle="Catalogue definitions and independent physical inventory." />
      <ComponentSections {...data} firearmId={validFirearm ? firearmId : ""} onFirearmChange={(value) => {
        const next = new URLSearchParams(searchParams);
        if (value) next.set("firearmId", value); else next.delete("firearmId");
        setSearchParams(next);
      }} />
    </div>
  );
}

function ComponentSections({ definitions, inventory, firearms, installations, firearmId, onFirearmChange }: ComponentsLoaderData & {
  firearmId: string;
  onFirearmChange: (value: string) => void;
}) {
  const { t } = useI18n();
  const [section, setSection] = useState<"inventory" | "catalogue">("inventory");
  const tabs = ["inventory", "catalogue"] as const;
  const tabRefs = useRef<Array<HTMLButtonElement | null>>([]);
  const selectTab = (next: "inventory" | "catalogue") => setSection(next);
  return (
    <div>
      <div className="component-tabs" role="tablist" aria-label={t("Component sections")}>
        {tabs.map((tab, index) => (
          <button
            key={tab}
            ref={(element) => { tabRefs.current[index] = element; }}
            className={`component-tab${section === tab ? " component-tab-active" : ""}`}
            type="button"
            role="tab"
            aria-selected={section === tab}
            aria-controls={`component-panel-${tab}`}
            id={`component-tab-${tab}`}
            tabIndex={section === tab ? 0 : -1}
            onClick={() => selectTab(tab)}
            onKeyDown={(event) => {
              if (!["ArrowLeft", "ArrowRight", "Home", "End"].includes(event.key)) return;
              event.preventDefault();
              const nextIndex = event.key === "Home" ? 0 : event.key === "End" ? tabs.length - 1
                : (index + (event.key === "ArrowRight" ? 1 : -1) + tabs.length) % tabs.length;
              const next = tabs[nextIndex];
              selectTab(next);
              tabRefs.current[nextIndex]?.focus();
            }}
          >
            {t(tab === "inventory" ? "My inventory" : "Catalogue")}
          </button>
        ))}
      </div>
      {section === "inventory" && (
        <div id="component-panel-inventory" role="tabpanel" aria-labelledby="component-tab-inventory">
          <Inventory {...{ inventory, definitions, firearms, installations, firearmId, onFirearmChange }} />
        </div>
      )}
      {section === "catalogue" && (
        <div id="component-panel-catalogue" role="tabpanel" aria-labelledby="component-tab-catalogue">
          <Catalogue items={definitions} />
        </div>
      )}
    </div>
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
  if (fetcher.state !== "idle" || !fetcher.data || fetcher.data.ok) return null;
  const message = typeof fetcher.data === "object" && fetcher.data && "error" in fetcher.data ? fetcher.data.error : "The request could not be completed.";
  return <p className="form-error" role="alert">{String(message)}</p>;
}

function SearchBox({ value, onChange, label }: { value: string; onChange: (value: string) => void; label: string }) {
  return <label className="search-field">{label}<input type="search" value={value} onChange={(event) => onChange(event.target.value)} /></label>;
}

function Catalogue({ items }: { items: WorkshopComponentDefinition[] }) {
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
      {visible.length ? <div className="table-wrap"><table><thead><tr><th>{t("Name")}</th><th>{t("Description")}</th><th>{t("Status")}</th><th>{t("Actions")}</th></tr></thead><tbody>{visible.map((item) => <tr key={item.id}><td><strong>{item.name}</strong></td><td>{item.description || t("No description")}</td><td><span className="badge">{t(item.archived ? "archived" : "active")}</span></td><td className="record-actions"><button className="button button-quiet" type="button" onClick={() => setModal({ item })}>{t("Edit")}</button><ManagementAction fetcher={fetcher} intent="archive-definition" item={item} label={t("Archive")} /><ManagementAction fetcher={fetcher} intent="delete-definition" item={item} label={t("Delete")} destructive /></td></tr>)}</tbody></table></div> : <EmptyState title={items.length ? t("No catalogue results") : t("Catalogue is empty")} />}
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

function Inventory({ inventory, definitions, firearms, installations, firearmId, onFirearmChange }: { inventory: WorkshopPhysicalComponent[]; definitions: WorkshopComponentDefinition[]; firearms: ComponentsLoaderData["firearms"]; installations: ComponentsLoaderData["installations"]; firearmId: string; onFirearmChange: (value: string) => void }) {
  const { culture, t } = useI18n();
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
      {visible.length ? <div className="table-wrap"><table><thead><tr><th>{t("Component")}</th><th>{t("Details")}</th><th>{t("Rounds")}</th><th>{t("Installed on")}</th><th>{t("Actions")}</th></tr></thead><tbody>{visible.map((item) => { const installation = installationByComponent.get(item.id); const firearm = installation ? firearmById.get(installation.firearmId) : undefined; return <tr key={item.id}><td><strong>{definitionById.get(item.definitionId)?.name ?? item.definitionId}</strong><small>{item.identificationNote || t("No identification note")}</small></td><td>{item.manufacturer || t("No manufacturer")} · {item.model || t("No model")}<small>{item.compatibilityTags.join(", ") || t("No compatibility tags")}</small></td><td>{formatNumber(item.totalRounds, culture)}</td><td>{firearm ? firearm.nickname || `${firearm.manufacturer} ${firearm.model}` : t("Uninstalled")}</td><td className="record-actions"><button className="button button-quiet" type="button" onClick={() => setModal({ item })}>{t("Edit")}</button><ManagementAction fetcher={fetcher} intent="archive-component" item={item} label={t("Archive")} /><ManagementAction fetcher={fetcher} intent="delete-component" item={item} label={t("Delete")} destructive /></td></tr>; })}</tbody></table></div> : <EmptyState title={inventory.length ? t("No inventory results") : t("No physical components")} />}
      {modal && <Modal title={t(modal.item ? "Edit physical component" : "Add physical component")} onClose={() => setModal(null)}><ComponentForm fetcher={fetcher} definitions={definitions} firearms={firearms} item={modal.item} installation={modal.item ? installationByComponent.get(modal.item.id) : undefined} initialFirearmId={firearmId === UNINSTALLED ? "" : firearmId} /></Modal>}
    </section>
  );
}

function ComponentForm({ fetcher, definitions, firearms, item, installation, initialFirearmId = "" }: { fetcher: ActionFetcher; definitions: WorkshopComponentDefinition[]; firearms: ComponentsLoaderData["firearms"]; item?: WorkshopPhysicalComponent; installation?: ComponentsLoaderData["installations"][number]; initialFirearmId?: string }) {
  const { t } = useI18n();
  const [trackingMode, setTrackingMode] = useState<WorkshopTrackingMode>(item?.trackingMode ?? "Counter");
  const [firearmId, setFirearmId] = useState(installation?.firearmId ?? initialFirearmId);
  const [tags, setTags] = useState(item?.compatibilityTags ?? []);
  const [tagInput, setTagInput] = useState("");
  const addTag = () => { const tag = tagInput.trim(); if (tag && !tags.includes(tag)) setTags((current) => [...current, tag]); setTagInput(""); };
  return <fetcher.Form method="post" className="form-panel"><input type="hidden" name="intent" value={item ? "update-component" : "create-component"} />{item && <><input type="hidden" name="id" value={item.id} /><input type="hidden" name="revision" value={item.revision} /><input type="hidden" name="installationId" value={installation?.id ?? ""} /><input type="hidden" name="installationRevision" value={installation?.revision ?? 0} /><input type="hidden" name="existingFirearmId" value={installation?.firearmId ?? ""} /></>}<label>{t("Definition")}<select name="definitionId" defaultValue={item?.definitionId ?? ""} required><option value="">{t("Select definition")}</option>{definitions.filter((definition) => !definition.archived).map((definition) => <option key={definition.id} value={definition.id}>{definition.name}</option>)}</select></label><div className="form-grid"><label>{t("Manufacturer")}<input name="manufacturer" defaultValue={item?.manufacturer ?? ""} /></label><label>{t("Model")}<input name="model" defaultValue={item?.model ?? ""} /></label></div><label>{t(item ? "Current round count" : "Initial round count")}<input type="number" min="0" step="1" name={item ? "totalRounds" : "initialRounds"} defaultValue={item?.totalRounds ?? 0} required /></label><label>{t("Compatibility tags")}<input value={tagInput} onChange={(event) => { const value = event.target.value; if (value.includes(",")) { const parts = value.split(","); setTagInput(parts.pop() ?? ""); setTags((current) => [...current, ...parts.map((part) => part.trim()).filter((part) => part && !current.includes(part))]); } else setTagInput(value); }} onKeyDown={(event) => { if (event.key === "," || event.key === "Enter") { event.preventDefault(); addTag(); } }} placeholder={t("Type a tag and press comma")} /><input type="hidden" name="compatibilityTags" value={tags.join(",")} /><span className="tag-list">{tags.map((tag) => <button className="tag-pill tag-pill-active" type="button" key={tag} onClick={() => setTags((current) => current.filter((value) => value !== tag))}>{tag} ×</button>)}</span></label><label>{t("Tracking mode")}<select name="trackingMode" value={trackingMode} onChange={(event) => setTrackingMode(event.target.value as WorkshopTrackingMode)}><option value="Counter">{t("Counter")}</option><option value="Interval">{t("Interval")}</option><option value="Manual">{t("Manual")}</option></select></label>{trackingMode === "Interval" && <label>{t("Maximum rounds")}<input type="number" min="1" name="serviceInterval" defaultValue={item?.serviceInterval ?? ""} required /></label>}<label>{t("Firearm (optional)")}<select name="firearmId" value={firearmId} onChange={(event) => setFirearmId(event.target.value)}><option value="">{t("Leave empty to add a spare part")}</option>{firearms.filter((firearm) => !firearm.archived).map((firearm) => <option key={firearm.id} value={firearm.id}>{firearm.nickname || `${firearm.manufacturer} ${firearm.model}`}</option>)}</select></label><label>{t("Identification note")}<small>{t("Short label used to identify this physical part.")}</small><input name="identificationNote" defaultValue={item?.identificationNote ?? ""} /></label><label>{t("Notes")}<small>{t("Additional maintenance or workshop context.")}</small><textarea name="notes" defaultValue={item?.notes ?? ""} /></label><ActionError fetcher={fetcher} /><button className="button button-primary" disabled={fetcher.state !== "idle"}>{t(item ? "Save" : "Add component")}</button></fetcher.Form>;
}
