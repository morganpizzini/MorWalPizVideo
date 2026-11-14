import { PageTitle } from "@morwalpiz/layout";
import { useWorkshopAuth } from "../../auth/AuthProvider";
export default function AccountProfile() {
  const { session } = useWorkshopAuth();
  return (
    <div>
      <PageTitle title="Account" subtitle="Shared MorWalPiz account identity" />
      <section className="panel">
        <dl className="definition-list">
          <dt>Username</dt>
          <dd>{session.account?.username}</dd>
          <dt>Email</dt>
          <dd>{session.account?.email}</dd>
          <dt>Display name</dt>
          <dd>{session.account?.displayName}</dd>
        </dl>
      </section>
    </div>
  );
}
