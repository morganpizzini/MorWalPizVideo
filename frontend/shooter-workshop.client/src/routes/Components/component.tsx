import { useEffect, useState } from "react";
import { useFetcher, useLoaderData } from "react-router";
import { EmptyState, PageTitle } from "@morwalpiz/layout";
import type { WorkshopComponentDefinition, WorkshopPhysicalComponent, WorkshopTrackingMode } from "@morwalpizvideo/models";
import { useI18n } from "../../i18n";
import { formatNumber } from "../../i18n-formatters";
import type { action } from "./action";
import type { ComponentsLoaderData } from "./loader";

type ActionFetcher = ReturnType<typeof useFetcher<typeof action>>;

export default function Components() {
  const { definitions, inventory, firearms } = useLoaderData() as ComponentsLoaderData;
  return (
    <div>
      <PageTitle title="Components" subtitle="Catalogue definitions and independent physical inventory." />
      <div className="detail-grid">
        <Catalogue items={definitions} />
        <Inventory items={inventory} definitions={definitions} firearms={firearms} />
      </div>
    </div>
  );
}

function DisclosureButton({ open, onClick, label }: { open: boolean; onClick: () => void; label: string }) {
  return (
    <button className="button button-quiet" type="button" aria-expanded={open} onClick={onClick}>
      {open ? `− ${label}` : `+ ${label}`}
    </button>
  );
}

function ActionError({ fetcher }: { fetcher: ActionFetcher }) {
  if (fetcher.state !== "idle" || !fetcher.data || fetcher.data.ok) return null;
  return <p className="form-error" role="alert">{String(fetcher.data)}</p>;
}

function Catalogue({ items }: { items: WorkshopComponentDefinition[] }) {
  const { t } = useI18n();
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<string | null>(null);
  const fetcher = useFetcher<typeof action>();
  const succeeded = fetcher.state === "idle" && fetcher.data?.ok;
  useEffect(() => {
    if (succeeded) {
      setOpen(false);
      setEditing(null);
    }
  }, [succeeded]);

  return (
    <section className="panel">
      <div className="section-heading">
        <h2>{t("Catalogue")}</h2>
        <DisclosureButton open={open} onClick={() => setOpen((value) => !value)} label={t("New component definition")} />
      </div>
      {open && (
        <fetcher.Form method="post" className="panel form-panel">
          <input type="hidden" name="intent" value="create-definition" />
          <h2>{t("New component definition")}</h2>
          <label>{t("Name")}<input name="name" required /></label>
          <label>{t("Description")}<textarea name="description" /></label>
          <label>{t("Notes")}<textarea name="notes" /></label>
          <ActionError fetcher={fetcher} />
          <button className="button button-primary" disabled={fetcher.state !== "idle"}>{t("Save definition")}</button>
        </fetcher.Form>
      )}
      {items.length ? (
        <ul className="compact-list">
          {items.map((item) => editing === item.id ? (
            <DefinitionEdit key={item.id} item={item} fetcher={fetcher} onCancel={() => setEditing(null)} />
          ) : (
            <li key={item.id}>
              <span><strong>{item.name}</strong><small>{item.description || t("No description")}</small></span>
              <span className="record-actions">
                <span className="badge">{t(item.archived ? "archived" : "active")}</span>
                {!item.archived && <button className="button button-quiet" type="button" onClick={() => setEditing(item.id)}>{t("Edit")}</button>}
                {!item.archived && <ManagementAction fetcher={fetcher} intent="archive-definition" item={item} label={t("Archive")} />}
                <ManagementAction fetcher={fetcher} intent="delete-definition" item={item} label={t("Delete")} destructive />
              </span>
            </li>
          ))}
        </ul>
      ) : <EmptyState title={t("Catalogue is empty")} />}
    </section>
  );
}

function DefinitionEdit({ item, fetcher, onCancel }: { item: WorkshopComponentDefinition; fetcher: ActionFetcher; onCancel: () => void }) {
  const { t } = useI18n();
  return (
    <li>
      <fetcher.Form method="post" className="form-panel">
        <input type="hidden" name="intent" value="update-definition" /><input type="hidden" name="id" value={item.id} /><input type="hidden" name="revision" value={item.revision} />
        <label>{t("Name")}<input name="name" defaultValue={item.name} required /></label>
        <label>{t("Description")}<textarea name="description" defaultValue={item.description ?? ""} /></label>
        <label>{t("Notes")}<textarea name="notes" defaultValue={item.notes ?? ""} /></label>
        <ActionError fetcher={fetcher} />
        <button className="button button-primary" disabled={fetcher.state !== "idle"}>{t("Save")}</button>
        <button className="button button-quiet" type="button" onClick={onCancel}>{t("Cancel")}</button>
      </fetcher.Form>
    </li>
  );
}

function ManagementAction({ fetcher, intent, item, label, destructive = false }: { fetcher: ActionFetcher; intent: string; item: { id: string; revision: number }; label: string; destructive?: boolean }) {
  const { t } = useI18n();
  return (
    <fetcher.Form method="post" onSubmit={(event) => {
      if (destructive && !window.confirm(t("This record has no removable history. Delete it? Archive is safer when it is in use."))) event.preventDefault();
    }}>
      <input type="hidden" name="intent" value={intent} /><input type="hidden" name="id" value={item.id} /><input type="hidden" name="revision" value={item.revision} />
      <button className="button button-quiet" type="submit" disabled={fetcher.state !== "idle"}>{label}</button>
    </fetcher.Form>
  );
}

function Inventory({ items, definitions, firearms }: { items: WorkshopPhysicalComponent[]; definitions: WorkshopComponentDefinition[]; firearms: ComponentsLoaderData["firearms"] }) {
  const { culture, t } = useI18n();
  const [open, setOpen] = useState(false);
  const [tagFilter, setTagFilter] = useState("");
  const [editing, setEditing] = useState<string | null>(null);
  const fetcher = useFetcher<typeof action>();
  const visible = tagFilter.trim() ? items.filter((item) => item.compatibilityTags.some((tag) => tag.toLowerCase().includes(tagFilter.trim().toLowerCase()))) : items;
  const succeeded = fetcher.state === "idle" && fetcher.data?.ok;
  useEffect(() => { if (succeeded) { setOpen(false); setEditing(null); } }, [succeeded]);
  return (
    <section className="panel">
      <div className="section-heading"><h2>{t("My inventory")}</h2><DisclosureButton open={open} onClick={() => setOpen((value) => !value)} label={t("Add physical component")} /></div>
      {open && <ComponentForm fetcher={fetcher} definitions={definitions} firearms={firearms} />}
      <div className="filter-row">
        <label>{t("Filter by compatibility tag")}<input value={tagFilter} onChange={(event) => setTagFilter(event.target.value)} placeholder={t("e.g. CZ")} /></label>
        {tagFilter && <button className="button button-quiet" type="button" onClick={() => setTagFilter("")}>{t("Clear")}</button>}
      </div>
      <ActionError fetcher={fetcher} />
      {visible.length ? <ul className="compact-list">{visible.map((item) => editing === item.id ? (
        <ComponentEdit key={item.id} item={item} definitions={definitions} fetcher={fetcher} onCancel={() => setEditing(null)} />
      ) : (
        <li key={item.id}>
          <span>
            <strong>{definitions.find((definition) => definition.id === item.definitionId)?.name ?? item.definitionId}</strong>
            <small>{item.identificationNote || t("No identification note")} · {item.manufacturer || t("No manufacturer")} · {item.compatibilityTags.join(", ") || t("No compatibility tags")} · {formatNumber(item.totalRounds, culture)} {t("rounds")}</small>
          </span>
          <span className="record-actions"><span className="badge">{t(item.archived ? "archived" : item.status)}</span>
            {!item.archived && <button className="button button-quiet" type="button" onClick={() => setEditing(item.id)}>{t("Edit")}</button>}
            {!item.archived && <ManagementAction fetcher={fetcher} intent="archive-component" item={item} label={t("Archive")} />}
            <ManagementAction fetcher={fetcher} intent="delete-component" item={item} label={t("Delete")} destructive />
          </span>
        </li>
      ))}</ul> : <EmptyState title={items.length ? t("No components match this tag") : t("No physical components")} />}
    </section>
  );
}

function ComponentForm({ fetcher, definitions, firearms, item }: { fetcher: ActionFetcher; definitions: WorkshopComponentDefinition[]; firearms: ComponentsLoaderData["firearms"]; item?: WorkshopPhysicalComponent }) {
  const { t } = useI18n();
  const [trackingMode, setTrackingMode] = useState<WorkshopTrackingMode>(item?.trackingMode ?? "Counter");
  return (
    <fetcher.Form method="post" className="panel form-panel">
      <input type="hidden" name="intent" value={item ? "update-component" : "create-component"} />
      {item && <><input type="hidden" name="id" value={item.id} /><input type="hidden" name="revision" value={item.revision} /></>}
      <h2>{t(item ? "Edit physical component" : "Add physical component")}</h2>
      <label>{t("Definition")}<select name="definitionId" defaultValue={item?.definitionId} required><option value="">{t("Select definition")}</option>{definitions.filter((definition) => !definition.archived).map((definition) => <option key={definition.id} value={definition.id}>{definition.name}</option>)}</select></label>
      <div className="form-grid">
        <label>{t("Manufacturer")}<input name="manufacturer" defaultValue={item?.manufacturer ?? ""} /></label>
        <label>{t("Model")}<input name="model" defaultValue={item?.model ?? ""} /></label>
      </div>
      <label>{t("Compatibility tags")}<input name="compatibilityTags" defaultValue={item?.compatibilityTags.join(", ") ?? ""} placeholder={t("Comma-separated tags")} /></label>
      <label>{t("Tracking mode")}<select name="trackingMode" value={trackingMode} onChange={(event) => setTrackingMode(event.target.value as WorkshopTrackingMode)}><option value="Counter">{t("Counter")}</option><option value="Interval">{t("Interval")}</option><option value="Manual">{t("Manual")}</option></select></label>
      {trackingMode === "Interval" && <label>{t("Maximum rounds")}<input type="number" min="1" name="serviceInterval" defaultValue={item?.serviceInterval ?? ""} required /></label>}
      <label>{t("Firearm (optional)")}<select name="firearmId" defaultValue=""><option value="">{t("Leave empty to add a spare part")}</option>{firearms.filter((firearm) => !firearm.archived).map((firearm) => <option key={firearm.id} value={firearm.id}>{firearm.nickname || `${firearm.manufacturer} ${firearm.model}`}</option>)}</select></label>
      <label>{t("Identification note")}<input name="identificationNote" defaultValue={item?.identificationNote ?? ""} /></label>
      <label>{t("Notes")}<textarea name="notes" defaultValue={item?.notes ?? ""} /></label>
      <ActionError fetcher={fetcher} /><button className="button button-primary" disabled={fetcher.state !== "idle"}>{t(item ? "Save" : "Add component")}</button>
    </fetcher.Form>
  );
}

function ComponentEdit({ item, definitions, fetcher, onCancel }: { item: WorkshopPhysicalComponent; definitions: WorkshopComponentDefinition[]; fetcher: ActionFetcher; onCancel: () => void }) {
  return <li><ComponentForm item={item} definitions={definitions} firearms={[]} fetcher={fetcher} /><button className="button button-quiet" type="button" onClick={onCancel}>Cancel</button></li>;
}
