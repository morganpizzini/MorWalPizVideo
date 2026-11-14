import { PageTitle } from "@morwalpiz/layout";
import { useWorkshopAuth } from "../../auth/AuthProvider";
import { useI18n } from "../../i18n";
export default function AccountProfile() {
  const { session } = useWorkshopAuth();
  const { t } = useI18n();
  return (
    <div>
      <PageTitle title={t("Account")} subtitle={t("Shared MorWalPiz account identity")} />
      <section className="panel">
        <dl className="definition-list">
          <dt>{t("Username")}</dt>
          <dd>{session.account?.username}</dd>
          <dt>{t("Email")}</dt>
          <dd>{session.account?.email}</dd>
          <dt>{t("Display name")}</dt>
          <dd>{session.account?.displayName}</dd>
        </dl>
      </section>
    </div>
  );
}
