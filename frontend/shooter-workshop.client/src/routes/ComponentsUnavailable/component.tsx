import { EmptyState } from "@morwalpiz/layout";
import { useI18n } from "../../i18n";
export default function ComponentsUnavailable() {
  const { t } = useI18n();
  return (
    <EmptyState
      title={t("Unavailable route")}
      message={t("Use the Components workspace for catalogue and inventory.")}
    />
  );
}
