import { PageTitle, TabbedWorkspace } from "@morwalpiz/layout";
import { Link, useFetcher, useLoaderData, useSearchParams } from "react-router";
import { useI18n } from "../../i18n";
import type { AmmunitionLoaderResources } from "./loader";
import Recipes from "../Recipes/component";
import AmmunitionPanel from "./AmmunitionPanel";
import { DeferredRoute } from "../shared";
import { CardGridSkeleton, PanelSkeleton } from "../../components/loading/LoadingPrimitives";
import type { ProductionActionResult } from "../submitProduction";
import ProductionForm from "../ProductionForm";
import type { OnlineInventoryAvailability } from "../onlineInventory";

export default function Ammunition() {
  const resources = useLoaderData() as AmmunitionLoaderResources;
  return <AmmunitionContent resources={resources} />;
}

function AmmunitionContent({ resources }: { resources: AmmunitionLoaderResources }) {
  const { t } = useI18n();
  const [searchParams, setSearchParams] = useSearchParams();
  const activeTab = searchParams.get("tab") === "recipes" ? "recipes" : "ammunition";
  const fetcher = useFetcher<ProductionActionResult>();

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
            panel: <DeferredRoute data={resources.productionPanel} fallback={<PanelSkeleton />} >{([recipes, batches, productionRecords]) => (
              <AmmunitionPanel
                batches={batches}
                productionRecords={productionRecords}
                renderProductionEntry={(record, minimumQuantity, onSaved, onCancel) => (
                  <DeferredRoute data={resources.inventory} fallback={<PanelSkeleton />}>
                    {(availability) => availability.inventory
                      ? (
                        <ProductionForm
                          key={record?.session.id ?? "new-production"}
                          recipes={recipes}
                          inventory={availability.inventory}
                          fetcher={fetcher}
                          record={record}
                          minimumQuantity={minimumQuantity}
                          onSaved={onSaved}
                          onCancel={onCancel}
                        />
                      )
                      : <InventoryUnavailable availability={availability} />}
                  </DeferredRoute>
                )}
              />
            )}</DeferredRoute>,
          },
          {
            key: "recipes",
            label: t("Recipes"),
            panel: (
              <DeferredRoute data={resources.recipes} fallback={<CardGridSkeleton />}>
                {(recipes) => (
                  <DeferredRoute
                    data={resources.inventory}
                    fallback={<Recipes recipes={recipes} inventory={null} inventoryLoading embedded />}
                  >
                    {(availability) => (
                      <Recipes recipes={recipes} inventory={availability.inventory} embedded />
                    )}
                  </DeferredRoute>
                )}
              </DeferredRoute>
            ),
          },
        ]}
      />
    </div>
  );
}

function InventoryUnavailable({ availability }: { availability: OnlineInventoryAvailability }) {
  const { t } = useI18n();
  return (
    <section className="panel" role="status">
      <h2>{availability.online ? t("Inventory summary unavailable") : t("Inventory is online-only")}</h2>
      <p>{availability.error ?? t("Reconnect to see current supply stock and finance totals.")}</p>
      <Link to="/inventory">{t("Open inventory")}</Link>
    </section>
  );
}
