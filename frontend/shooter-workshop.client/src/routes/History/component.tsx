import { PageTitle, EmptyState } from "@morwalpiz/layout";
import { useLoaderData } from "react-router";
import type { WorkshopActivity } from "@morwalpizvideo/models";
import { ActivityList } from "../shared";
import { useI18n } from "../../i18n";

export default function History() {
  const { t } = useI18n();
  const { items } = useLoaderData() as { items: WorkshopActivity[] };
  return (
    <div>
      <PageTitle title={t("History")} subtitle={t("Account-filtered chronological activity from the controller.")} />
      {items.length ? (
        <section className="panel"><ActivityList items={items} /></section>
      ) : <EmptyState title={t("No history yet")} />}
    </div>
  );
}
