import { useEffect, useState } from "react";
import { Link, useFetcher, useLoaderData } from "react-router";
import { PageTitle, EmptyState } from "@morwalpiz/layout";
import type { CreateWorkshopFirearmRequest } from "@morwalpizvideo/models";
import type { action } from "./action";
import type { FirearmsLoaderData } from "./loader";
import { useI18n } from "../../i18n";
import { formatNumber } from "../../i18n-formatters";

export default function Firearms() {
  const { culture, t } = useI18n();
  const { firearms: items } = useLoaderData() as FirearmsLoaderData;
  const [showForm, setShowForm] = useState(false);
  const fetcher = useFetcher<typeof action>();
  const succeeded = fetcher.state === "idle" && fetcher.data?.ok;
  useEffect(() => {
    if (succeeded) setShowForm(false);
  }, [succeeded]);

  return (
    <div>
          <PageTitle
            title={t("My firearms")}
            subtitle={t("{count} account-owned records", { count: items.length })}
          />
          <button
            className="button button-primary"
            onClick={() => setShowForm(!showForm)}
          >
            + {t("Register firearm")}
          </button>
          {showForm && (
            <FirearmForm fetcher={fetcher} />
          )}
          {items.length ? (
            <div className="card-grid mt-3">
              {items.map((firearm) => (
                <Link
                  className="record-card"
                  to={`/firearms/${firearm.id}`}
                  key={firearm.id}
                >
                  <div className="record-card-top">
                    <span className="eyebrow">{firearm.caliber}</span>
                    <span className="badge">
                      {t(firearm.archived ? "archived" : "active")}
                    </span>
                  </div>
                  <h2>
                    {firearm.nickname ||
                      `${firearm.manufacturer} ${firearm.model}`}
                  </h2>
                  <p>
                    {firearm.manufacturer} {firearm.model}
                  </p>
                  <strong>{formatNumber(firearm.totalRounds, culture)} {t("rounds")}</strong>
                </Link>
              ))}
            </div>
          ) : (
            <EmptyState
              title={t("No firearms registered")}
              message={t("Add a firearm to connect sessions, components and allocations.")}
            />
          )}
    </div>
  );
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
        <p className="form-error" role="alert">{fetcher.data.error}</p>
      )}
      <button className="button button-primary" disabled={fetcher.state !== "idle"}>
        {fetcher.state === "submitting" ? t("Saving…") : t("Save firearm")}
      </button>
    </fetcher.Form>
  );
}
