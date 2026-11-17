import { PageTitle, TabbedWorkspace } from "@morwalpiz/layout";
import { useFetcher, useLoaderData, useSearchParams } from "react-router";
import { useI18n } from "../../i18n";
import type { action } from "./action";
import type { AmmunitionLoaderData } from "./loader";
import Recipes from "../Recipes/component";
import AmmunitionPanel from "./AmmunitionPanel";

export default function Ammunition() {
  const { t } = useI18n();
  const { recipes, batches } = useLoaderData() as AmmunitionLoaderData;
  const [searchParams, setSearchParams] = useSearchParams();
  const activeTab = searchParams.get("tab") === "recipes" ? "recipes" : "ammunition";
  const fetcher = useFetcher<typeof action>();

  return (
    <div>
      <PageTitle
        title={t("Ammunition")}
        subtitle={t("Manage ammunition production and recipes.")}
      />
      <TabbedWorkspace
        aria-label={t("Ammunition sections")}
        selectedKey={activeTab}
        onSelectedKeyChange={(key) => {
          const nextSearchParams = new URLSearchParams(searchParams);
          if (key === "recipes") nextSearchParams.set("tab", key);
          else nextSearchParams.delete("tab");
          setSearchParams(nextSearchParams);
        }}
        tabs={[
          {
            key: "ammunition",
            label: t("Ammunition"),
            panel: <AmmunitionPanel recipes={recipes} batches={batches} fetcher={fetcher} />,
          },
          { key: "recipes", label: t("Recipes"), panel: <Recipes recipes={recipes} embedded /> },
        ]}
      />
    </div>
  );
}
