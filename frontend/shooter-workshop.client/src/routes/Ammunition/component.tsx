import { PageTitle, TabbedWorkspace } from "@morwalpiz/layout";
import { useFetcher, useLoaderData, useSearchParams } from "react-router";
import { useI18n } from "../../i18n";
import type { action } from "./action";
import type { AmmunitionLoaderResources } from "./loader";
import Recipes from "../Recipes/component";
import AmmunitionPanel from "./AmmunitionPanel";
import { DeferredRoute } from "../shared";
import { CardGridSkeleton, PanelSkeleton } from "../../components/loading/LoadingPrimitives";

export default function Ammunition() {
  const resources = useLoaderData() as AmmunitionLoaderResources;
  return <AmmunitionContent resources={resources} />;
}

function AmmunitionContent({ resources }: { resources: AmmunitionLoaderResources }) {
  const { t } = useI18n();
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
            panel: <DeferredRoute data={resources.productionPanel} fallback={<PanelSkeleton />} >{([recipes, batches]) => <AmmunitionPanel recipes={recipes} batches={batches} fetcher={fetcher} />}</DeferredRoute>,
          },
          { key: "recipes", label: t("Recipes"), panel: <DeferredRoute data={resources.recipes} fallback={<CardGridSkeleton />} >{(recipes) => <Recipes recipes={recipes} embedded />}</DeferredRoute> },
        ]}
      />
    </div>
  );
}
