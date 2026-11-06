import {
  useState,
  type FormEvent
} from "react";
import {
  useLoaderData,
  useRevalidator
} from "react-router-dom";


import {
  freezerApi,
  type Profile,
  type ReferenceData,
  type ShooterIdentity
} from "../freezerApi";


import {
  ErrorMessage,
  DivisionSelect,
  errorMessage
} from "./shared";

export function ProfilePage() {
  const data = useLoaderData() as {
    profile: Profile;
    identities: ShooterIdentity[];
    referenceData: ReferenceData;
  } | null;
  const profile = data?.profile;
  const revalidator = useRevalidator();
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [creatingIdentity, setCreatingIdentity] = useState(false);

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!profile) {
      setError("Accedi per modificare il profilo.");
      return;
    }
    setBusy(true);
    setError("");
    setMessage("");
    const form = new FormData(event.currentTarget);
    try {
      await freezerApi.saveProfile({
        identityId: String(form.get("identityId") ?? "") || null,
        division: String(form.get("division") ?? "") || null,
        powerFactor: String(form.get("powerFactor") ?? "") || null,
        performance: Number(form.get("performance")),
      });
      setMessage("Profilo aggiornato.");
      revalidator.revalidate();
    } catch (cause) {
      setError(errorMessage(cause));
    } finally {
      setBusy(false);
    }
  }

  async function createIdentity(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formElement = event.currentTarget;
    setError("");
    setMessage("");
    setCreatingIdentity(true);
    const form = new FormData(formElement);
    const aliases = String(form.get("aliases") ?? "")
      .split(",")
      .map((value) => value.trim())
      .filter(Boolean);
    try {
      const identity = await freezerApi.createIdentity(
        String(form.get("displayName") ?? ""),
        aliases,
      );
      setMessage(
        `Identità creata: ${identity.aliases[0] ?? identity.normalizedName}. Associala ai risultati dopo averli verificati.`,
      );
      formElement.reset();
      revalidator.revalidate();
    } catch (cause) {
      setError(errorMessage(cause));
    } finally {
      setCreatingIdentity(false);
    }
  }

  return (
    <section className="form-panel">
      <h1>Profilo tiratore</h1>
      <p className="lede">
        Queste impostazioni sono private e non modificano le classifiche
        condivise.
      </p>
      <form className="form-stack" onSubmit={(event) => void save(event)}>
        {!profile && (
          <ErrorMessage>
            Accedi per visualizzare e modificare il profilo.
          </ErrorMessage>
        )}
        <label>
          Identità agonistica
          <select
            name="identityId"
            defaultValue={profile?.identityId ?? ""}
            disabled={!profile}
          >
            <option value="">Non associata</option>
            {(data?.identities ?? []).map((identity) => (
              <option key={identity.id} value={identity.id}>
                {identity.aliases[0] ?? identity.normalizedName}
              </option>
            ))}
          </select>
        </label>
        <p className="quiet-copy">
          Le identità si creano e si associano dalla revisione della gara;
          nessuna corrispondenza viene applicata automaticamente.
        </p>
        <div className="form-columns">
          <label>
            Divisione
            <DivisionSelect
              options={data?.referenceData.divisions ?? []}
              value={profile?.division}
              required
            />
          </label>
          <label>
            Power factor
            <select
              name="powerFactor"
              defaultValue={profile?.powerFactor ?? ""}
            >
              <option value="">Non impostato</option>
              <option>Major</option>
              <option>Minor</option>
            </select>
          </label>
        </div>
        <label>
          Performance personale
          <input
            name="performance"
            type="number"
            min="0.01"
            max="1"
            step="0.001"
            defaultValue={profile?.performance ?? 0.935}
          />
        </label>
        <p className="field-hint">
          È la quota attesa dei punti massimi dello stage usata per stimare il
          costo temporale dei punti persi. 0,935 significa 93,5%; non è una
          posizione in classifica né una probabilità di previsione.
        </p>
        {error && <ErrorMessage>{error}</ErrorMessage>}
        {message && (
          <p className="notice notice-success" role="status">
            {message}
          </p>
        )}
        <button className="button button-primary" disabled={busy}>
          {busy ? "Salvataggio…" : "Salva profilo"}
        </button>
      </form>
      <form
        className="form-stack identity-create-form"
        onSubmit={(event) => void createIdentity(event)}
      >
        <h2>Crea un’identità agonistica</h2>
        <p className="quiet-copy">
          Gli alias vengono aggiunti solo su tua conferma; non associano
          automaticamente i risultati.
        </p>
        <label>
          Nome principale
          <input
            name="displayName"
            required
            maxLength={100}
            disabled={creatingIdentity}
          />
        </label>
        <label>
          Altri nomi confermati
          <input
            name="aliases"
            maxLength={2000}
            disabled={creatingIdentity}
            placeholder="Un alias, un alias alternativo"
          />
          <span className="field-hint">
            Separali con una virgola. Questo flusso non acquisisce licenze:
            inserisci solo nomi alternativi che puoi confermare.
          </span>
        </label>
        <button
          className="button button-secondary"
          disabled={creatingIdentity || !profile}
        >
          {creatingIdentity ? "Creazione…" : "Crea identità"}
        </button>
      </form>
    </section>
  );
}

