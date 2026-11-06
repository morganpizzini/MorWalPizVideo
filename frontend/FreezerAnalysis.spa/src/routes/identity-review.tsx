import {
  useState,
  type FormEvent
} from "react";
import {
  Link,
  useLoaderData,
  useRevalidator,
  useParams
} from "react-router-dom";


import {
  freezerApi,
  FreezerApiError,
  type IdentityReviewEntry
} from "../freezerApi";


import {
  ErrorMessage,
  errorMessage
} from "./shared";

interface IdentityReviewLoaderData {
  entries: IdentityReviewEntry[] | null;
  offline: boolean;
}

export function IdentityReviewPage() {
  const { matchId } = useParams();
  const data = useLoaderData() as IdentityReviewLoaderData | null;
  const revalidator = useRevalidator();
  const [busyResultId, setBusyResultId] = useState("");
  const [error, setError] = useState("");
  const [status, setStatus] = useState("");

  async function assign(
    event: FormEvent<HTMLFormElement>,
    entry: IdentityReviewEntry,
  ) {
    event.preventDefault();
    if (!matchId) return;
    const form = new FormData(event.currentTarget);
    const identityId = String(form.get("identityId") ?? "");
    if (!identityId) {
      setError("Seleziona un’identità prima di confermare l’associazione.");
      return;
    }

    setBusyResultId(entry.resultId);
    setError("");
    setStatus("");
    try {
      await freezerApi.resolveIdentity(
        matchId,
        entry.resultId,
        identityId,
        entry.resolutionRevision,
      );
      setStatus(`Associazione aggiornata per ${entry.displayName}.`);
      revalidator.revalidate();
    } catch (cause) {
      setError(errorMessage(cause));
      if (cause instanceof FreezerApiError && cause.status === 409)
        revalidator.revalidate();
    } finally {
      setBusyResultId("");
    }
  }

  async function createAndAssign(entry: IdentityReviewEntry) {
    if (!matchId) return;
    setBusyResultId(entry.resultId);
    setError("");
    setStatus("");
    try {
      const identity = await freezerApi.createIdentity(entry.displayName);
      await freezerApi.resolveIdentity(
        matchId,
        entry.resultId,
        identity.id,
        entry.resolutionRevision,
      );
      setStatus(
        `Creata e associata una nuova identità per ${entry.displayName}.`,
      );
      revalidator.revalidate();
    } catch (cause) {
      setError(errorMessage(cause));
    } finally {
      setBusyResultId("");
    }
  }

  async function remove(entry: IdentityReviewEntry) {
    if (!matchId) return;
    setBusyResultId(entry.resultId);
    setError("");
    setStatus("");
    try {
      await freezerApi.removeIdentityResolution(
        matchId,
        entry.resultId,
        entry.resolutionRevision,
      );
      setStatus(`Associazione rimossa per ${entry.displayName}.`);
      revalidator.revalidate();
    } catch (cause) {
      setError(errorMessage(cause));
      if (cause instanceof FreezerApiError && cause.status === 409)
        revalidator.revalidate();
    } finally {
      setBusyResultId("");
    }
  }

  return (
    <div className="content-grid">
      <Link
        to={matchId ? `/matches/${encodeURIComponent(matchId)}` : "/"}
        className="back-link"
      >
        Torna alla preparazione
      </Link>
      <section className="section-heading">
        <div>
          <h1>Revisione identità</h1>
          <p className="lede">
            Associa i risultati a identità stabili solo dopo aver verificato
            nome e divisione. Le associazioni restano private al tuo account.
          </p>
        </div>
      </section>
      {data?.offline && (
        <div className="notice notice-warning" role="status">
          La revisione identità richiede una connessione attiva.
        </div>
      )}
      {error && <ErrorMessage>{error}</ErrorMessage>}
      {status && (
        <p className="notice notice-success" role="status">
          {status}
        </p>
      )}
      {data?.entries && data.entries.length === 0 && (
        <div className="empty-state">
          <h2>Nessun risultato da associare.</h2>
          <p>
            Pubblica una revisione della gara prima di rivedere le identità.
          </p>
        </div>
      )}
      {data?.entries && data.entries.length > 0 && (
        <ol
          className="identity-list"
          aria-label="Risultati della gara da verificare"
        >
          {data.entries.map((entry) => {
            const busy = busyResultId === entry.resultId;
            return (
              <li className="identity-row" key={entry.resultId}>
                <div className="identity-result">
                  <h2>{entry.displayName}</h2>
                  <p>
                    {entry.division} · ID locale {entry.competitorId}
                  </p>
                  <span
                    className={
                      entry.identityId
                        ? "status status-final"
                        : "status status-warning"
                    }
                  >
                    {entry.identityId ? "Associata" : "Da associare"}
                  </span>
                  {entry.identityId && (
                    <small className="identity-reference">
                      Identità {entry.identityId.slice(-8)} · revisione{" "}
                      {entry.resolutionRevision}
                    </small>
                  )}
                </div>
                <div className="identity-actions">
                  <form
                    className="identity-controls"
                    onSubmit={(event) => void assign(event, entry)}
                  >
                    <label>
                      Identità confermata
                      <select
                        name="identityId"
                        defaultValue={entry.identityId ?? ""}
                        disabled={busy}
                        required
                      >
                        <option value="">
                          Scegli una corrispondenza esatta
                        </option>
                        {entry.candidates.map((identity) => (
                          <option key={identity.id} value={identity.id}>
                            {identity.aliases[0] ?? identity.normalizedName} ·{" "}
                            {identity.id.slice(-8)}
                          </option>
                        ))}
                      </select>
                    </label>
                    <button
                      className="button button-secondary"
                      disabled={busy || entry.candidates.length === 0}
                    >
                      {busy ? "Salvataggio…" : "Conferma associazione"}
                    </button>
                  </form>
                  <div className="identity-secondary-actions">
                    <button
                      className="button button-secondary"
                      disabled={busy}
                      onClick={() => void createAndAssign(entry)}
                    >
                      Crea identità distinta
                    </button>
                    {entry.identityId && (
                      <button
                        className="button button-quiet"
                        disabled={busy}
                        onClick={() => void remove(entry)}
                      >
                        Rimuovi associazione
                      </button>
                    )}
                  </div>
                </div>
              </li>
            );
          })}
        </ol>
      )}
    </div>
  );
}

