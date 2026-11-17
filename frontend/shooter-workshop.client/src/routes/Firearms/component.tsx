import { useEffect, useState } from "react";
import { Link, useFetcher, useLoaderData, useSearchParams } from "react-router";
import { PageTitle, TabbedWorkspace, EmptyState } from "@morwalpiz/layout";
import type { CreateWorkshopFirearmRequest } from "@morwalpizvideo/models";
import type { action } from "./action";
import type { EquipmentLoaderData } from "./loader";
import { useI18n } from "../../i18n";
import { formatNumber } from "../../i18n-formatters";
import InventoryPanel from "./InventoryPanel";
import CataloguePanel from "./CataloguePanel";
import { DeferredRoute, type DeferredRouteData } from "../shared";

export default function Firearms() {
  const { data } = useLoaderData() as DeferredRouteData<EquipmentLoaderData>;
  return <DeferredRoute data={data}>{(resolved) => <FirearmsContent data={resolved} />}</DeferredRoute>;
}

function FirearmsContent({ data }: { data: EquipmentLoaderData }) {
  const { t } = useI18n();
  const [searchParams, setSearchParams] = useSearchParams();
  const rawTab = searchParams.get("tab");
  const activeTab = rawTab === "inventory" || rawTab === "catalogue" ? rawTab : "firearms";
  const firearmId = searchParams.get("firearmId") ?? "";
  const validFirearm = firearmId === "__uninstalled" || data.firearms.some((firearm) => firearm.id === firearmId);
  const [showForm, setShowForm] = useState(false);
  const fetcher = useFetcher<typeof action>();
  const succeeded = fetcher.state === "idle" && fetcher.data?.ok;
  useEffect(() => {
    if (succeeded) setShowForm(false);
  }, [succeeded]);

  useEffect(() => {
    const next = new URLSearchParams(searchParams);
    let changed = false;
    if (rawTab !== null && !["inventory", "catalogue"].includes(rawTab)) {
      next.delete("tab");
      changed = true;
    }
    if (firearmId && firearmId !== "__uninstalled" && !validFirearm) {
      next.delete("firearmId");
      changed = true;
    }
    if (changed) {
      setSearchParams(next, { replace: true });
    }
  }, [firearmId, rawTab, validFirearm, searchParams, setSearchParams]);

  const selectTab = (key: string) => {
    const next = new URLSearchParams(searchParams);
    if (key === "firearms") next.delete("tab");
    else next.set("tab", key);
    setSearchParams(next);
  };

  return (
    <div>
          <PageTitle
            title={t("Equipment")}
            subtitle={t("Manage firearms, physical inventory and component definitions.")}
          />
          <TabbedWorkspace
            aria-label={t("Equipment sections")}
            selectedKey={activeTab}
            onSelectedKeyChange={selectTab}
            tabs={[
              { key: "firearms", label: t("Firearms"), panel: <FirearmsPanel items={data.firearms} showForm={showForm} onToggleForm={() => setShowForm(!showForm)} fetcher={fetcher} /> },
              { key: "inventory", label: t("Inventory"), panel: <InventoryPanel inventory={data.inventory} definitions={data.definitions} firearms={data.firearms} installations={data.installations} firearmId={validFirearm ? firearmId : ""} onFirearmChange={(value) => {
                const next = new URLSearchParams(searchParams);
                if (value) next.set("firearmId", value); else next.delete("firearmId");
                setSearchParams(next);
              }} /> },
              { key: "catalogue", label: t("Catalogue"), panel: <CataloguePanel items={data.definitions} /> },
            ]}
          />
    </div>
  );
}

function FirearmsPanel({ items, showForm, onToggleForm, fetcher }: { items: EquipmentLoaderData["firearms"]; showForm: boolean; onToggleForm: () => void; fetcher: ReturnType<typeof useFetcher<typeof action>> }) {
  const { t } = useI18n();
  const { culture } = useI18n();
  return <section>
    <button className="button button-primary" type="button" onClick={onToggleForm}>+ {t("Register firearm")}</button>
    {showForm && <FirearmForm fetcher={fetcher} />}
    {items.length ? <div className="card-grid mt-3">{items.map((firearm) => <Link className="record-card" to={`/firearms/${firearm.id}`} key={firearm.id}><div className="record-card-top"><span className="eyebrow">{firearm.caliber}</span><span className="badge">{t(firearm.archived ? "archived" : "active")}</span></div><h2>{firearm.nickname || `${firearm.manufacturer} ${firearm.model}`}</h2><p>{firearm.manufacturer} {firearm.model}</p><strong>{formatNumber(firearm.totalRounds, culture)} {t("rounds")}</strong></Link>)}</div> : <EmptyState title={t("No firearms registered")} message={t("Add a firearm to connect sessions, components and allocations.")} />}
  </section>;
}

function FirearmForm({ fetcher }: { fetcher: ReturnType<typeof useFetcher<typeof action>> }) {
  const { t } = useI18n();
  const [form, setForm] = useState<CreateWorkshopFirearmRequest>({
    manufacturer: "",
    model: "",
    caliber: "",
    initialRounds: 0,
    nickname: "",
    category: "",
    notes: "",
    idempotencyKey: crypto.randomUUID(),
  });
  return (
    <fetcher.Form method="post" className="panel form-panel">
      <input type="hidden" name="intent" value="create-firearm" />
      <input type="hidden" name="idempotencyKey" value={form.idempotencyKey} />
      <h2>{t("Register firearm")}</h2>
      <div className="form-grid">
        {(
          ["manufacturer", "model", "caliber", "nickname", "category"] as const
        ).map((field) => (
          <label key={field}>
            {t(field)}
            <input
              name={field}
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
          name="initialRounds"
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
          name="notes"
          value={form.notes ?? ""}
          onChange={(event) => setForm({ ...form, notes: event.target.value })}
        />
      </label>
      {fetcher.state === "idle" && fetcher.data && !fetcher.data.ok && (
        <p className="form-error" role="alert">{"error" in fetcher.data ? fetcher.data.error : t("The request could not be completed.")}</p>
      )}
      <button className="button button-primary" disabled={fetcher.state !== "idle"}>
        {fetcher.state === "submitting" ? t("Saving…") : t("Save firearm")}
      </button>
    </fetcher.Form>
  );
}
