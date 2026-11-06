import {
  useState,
  type FormEvent
} from "react";
import {
  Link,
  useLoaderData,
  useNavigate,
  useRevalidator,
  useParams
} from "react-router-dom";

import {
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis
} from "recharts";
import {
  freezerApi,
  type Draft,
  type DraftUpdate,
  type Estimate,
  type Profile,
  type ReferenceData,
  type ResultDetail,
  type ResultSummary,
  type ShooterIdentity
} from "../freezerApi";
import {
  saveSnapshot
} from "../offlineStore";

import {
  rootData,
  ErrorMessage,
  ContextHelp,
  DivisionSelect,
  errorMessage
} from "./shared";

interface MatchLoaderData {
  draft: Draft;
  estimate: Estimate;
  profile: Profile;
  identities: ShooterIdentity[];
  referenceData: ReferenceData;
  results: ResultSummary[];
}

function ResultWorkspace({
  matchId,
  results,
}: {
  matchId: string;
  results: ResultSummary[];
}) {
  const [filter, setFilter] = useState("");
  const [selected, setSelected] = useState<string[]>([]);
  const [details, setDetails] = useState<Record<string, ResultDetail>>({});
  const [busy, setBusy] = useState("");
  const [detailError, setDetailError] = useState("");
  const visible = results.filter(
    (result) =>
      !filter || result.division === filter || result.status === filter,
  );

  async function loadDetail(resultId: string) {
    if (details[resultId]) return;
    setBusy(resultId);
    setDetailError("");
    try {
      const detail = await freezerApi.getResult(matchId, resultId);
      setDetails((current) => ({ ...current, [resultId]: detail }));
    } catch (cause) {
      setDetailError(errorMessage(cause));
    } finally {
      setBusy("");
    }
  }

  const first = details[selected[0]];
  const second = details[selected[1]];
  const chartRows =
    first && second
      ? first.stages
          .map((stage) => {
            const paired = second.stages.find(
              (item) => item.stageId === stage.stageId,
            );
            return {
              stage: `Stage ${stage.stageNumber}`,
              [first.summary.displayName]:
                stage.status === "Scored" && Number.isFinite(stage.hitFactor)
                  ? stage.hitFactor
                  : undefined,
              [second.summary.displayName]:
                paired?.status === "Scored" && Number.isFinite(paired.hitFactor)
                  ? paired.hitFactor
                  : undefined,
            };
          })
          .sort(
            (left, right) =>
              Number(left.stage.replace("Stage ", "")) -
              Number(right.stage.replace("Stage ", "")),
          )
      : [];

  return (
    <section className="panel result-workspace">
      <div className="panel-heading">
        <div>
          <h2>Risultati <ContextHelp id="results-help">I risultati appartengono alla revisione pubblicata attiva e sono consultabili senza modificare il piano congelato.</ContextHelp></h2>
          <p className="quiet-copy">
            La revisione attiva è di sola lettura. I dettagli Verify si aprono
            per singolo atleta.
          </p>
        </div>
      </div>
      <label>
        Filtra divisione o stato
        <select
          value={filter}
          onChange={(event) => setFilter(event.currentTarget.value)}
        >
          <option value="">Tutti</option>
          {[...new Set(results.map((result) => result.division))].map(
            (value) => (
              <option key={value} value={value}>
                {value}
              </option>
            ),
          )}
          {["Scored", "Disqualified", "Zeroed", "Missing"].map((value) => (
            <option key={value} value={value}>
              {value}
            </option>
          ))}
        </select>
      </label>
      <div className="pdf-table-scroll">
        <table>
          <thead>
            <tr>
              <th>Atleta</th>
              <th>Divisione</th>
              <th>Classe</th>
              <th>Squadra</th>
              <th>Totale</th>
              <th>Stato</th>
              <th>Dettagli</th>
            </tr>
          </thead>
          <tbody>
            {visible.map((result) => (
              <tr key={result.resultId}>
                <th scope="row">{result.displayName}</th>
                <td>{result.division}</td>
                <td>{result.class ?? "—"}</td>
                <td>{result.squad ?? "—"}</td>
                <td>{result.officialTotal?.toFixed(2) ?? "—"}</td>
                <td>{result.status}</td>
                <td>
                  <button
                    className="button button-quiet"
                    onClick={() => void loadDetail(result.resultId)}
                  >
                    {busy === result.resultId ? "Caricamento…" : "Apri Verify"}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div className="comparison-controls">
        <h3>Confronto HF <ContextHelp id="hf-help">Confronta l’hit factor stage per stage tra due atleti distinti. I dati mancanti restano vuoti e non vengono trattati come zero.</ContextHelp></h3>
        <p className="quiet-copy">
          Seleziona esattamente due atleti della revisione attiva.
        </p>
        <select
          value={selected[0] ?? ""}
          onChange={(event) => {
            const value = event.currentTarget.value;
            setSelected((current) => [value, current[1]]);
            if (value) void loadDetail(value);
          }}
        >
          <option value="">Primo atleta</option>
          {results.map((result) => (
            <option key={result.resultId} value={result.resultId}>
              {result.displayName}
            </option>
          ))}
        </select>
        <select
          value={selected[1] ?? ""}
          onChange={(event) => {
            const value = event.currentTarget.value;
            setSelected((current) => [current[0] ?? "", value]);
            if (value) void loadDetail(value);
          }}
        >
          <option value="">Secondo atleta</option>
          {results.map((result) => (
            <option key={result.resultId} value={result.resultId}>
              {result.displayName}
            </option>
          ))}
        </select>
      </div>
      {detailError && <ErrorMessage>{detailError}</ErrorMessage>}
      {first &&
        second &&
        first.summary.resultId !== second.summary.resultId && (
          <>
            <div
              className="chart-scroll"
              aria-label="Confronto hit factor per stage"
            >
              <ResponsiveContainer
                width={Math.max(520, chartRows.length * 120)}
                height={280}
              >
                <BarChart data={chartRows}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="stage" />
                  <YAxis />
                  <Tooltip />
                  <Legend />
                  <Bar dataKey={first.summary.displayName} fill="#236b5a" />
                  <Bar dataKey={second.summary.displayName} fill="#c56b3f" />
                </BarChart>
              </ResponsiveContainer>
            </div>
            <div className="pdf-table-scroll">
              <table>
                <thead>
                  <tr>
                    <th>Stage</th>
                    <th>{first.summary.displayName} HF</th>
                    <th>{second.summary.displayName} HF</th>
                    <th>Stato</th>
                  </tr>
                </thead>
                <tbody>
                  {chartRows.map((row) => (
                    <tr key={row.stage}>
                      <th scope="row">{row.stage}</th>
                      <td>{row[first.summary.displayName] ?? "—"}</td>
                      <td>{row[second.summary.displayName] ?? "—"}</td>
                      <td>
                        {row[first.summary.displayName] === undefined ||
                        row[second.summary.displayName] === undefined
                          ? "Dati incompleti"
                          : "Scored"}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </>
        )}
    </section>
  );
}

export function MatchPage() {
  const { matchId } = useParams();
  const data = useLoaderData() as MatchLoaderData | null;
  const draft = data?.draft;
  const estimate = data?.estimate;
  const profile = data?.profile;
  const account = rootData().session.account;
  const navigate = useNavigate();
  const revalidator = useRevalidator();
  const [error, setError] = useState("");
  const [status, setStatus] = useState("");
  const [busy, setBusy] = useState(false);

  async function saveAndFreeze(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!matchId || !draft) {
      setError("Accedi per modificare il piano.");
      return;
    }

    setBusy(true);
    setError("");
    const form = new FormData(event.currentTarget);
    const strategies = { ...draft.strategyByStage };
    for (const stage of estimate?.stages ?? []) {
      strategies[stage.stageId] = String(
        form.get(`strategy:${stage.stageId}`) ?? "",
      );
    }
    const request: DraftUpdate = {
      expectedRevision: draft.revision,
      importManifestId: draft.importManifestId,
      identityId: String(form.get("identityId") ?? "") || null,
      division: String(form.get("division") ?? "") || null,
      beta: Number(form.get("beta")),
      driftPercent: Number(form.get("driftPercent")),
      ownVariability: Number(form.get("ownVariability")),
      distancePenalty: Number(form.get("distancePenalty")),
      performance: Number(form.get("performance")),
      applyAfternoon: form.get("applyAfternoon") === "on",
      applyOneHand: form.get("applyOneHand") === "on",
      excludedStageIds: (estimate?.stages ?? [])
        .filter((stage) => form.get(`exclude:${stage.stageId}`) === "on")
        .map((stage) => stage.stageId),
      rivalIdentityIds: draft.rivalIdentityIds,
      strategyByStage: strategies,
    };
    let draftSaved = false;
    try {
      const updated = await freezerApi.saveDraft(matchId, request);
      draftSaved = true;
      const submitter = (event.nativeEvent as SubmitEvent).submitter as HTMLButtonElement | null;
      if (submitter?.value === "save") {
        setStatus(`Bozza salvata alla revisione ${updated.revision}.`);
        revalidator.revalidate();
        return;
      }

      const snapshot = await freezerApi.freeze(matchId, updated.revision);
      if (!account?.id)
        throw new Error(
          "Account non disponibile per salvare la copia offline.",
        );
      try {
        await saveSnapshot(account.id, snapshot);
      } catch (storageError) {
        setError(
          `Piano congelato sul server, ma la copia offline non è stata salvata: ${errorMessage(storageError)}`,
        );
        revalidator.revalidate();
        return;
      }
      navigate(`/field/${encodeURIComponent(snapshot.id)}`);
    } catch (cause) {
      setError(errorMessage(cause));
      if (draftSaved) revalidator.revalidate();
    } finally {
      setBusy(false);
    }
  }

  async function reopenDraft() {
    if (!matchId || !draft?.frozenSnapshotId) return;
    setBusy(true);
    setError("");
    try {
      await freezerApi.reopenDraft(matchId, draft.revision);
      setStatus("Piano riaperto: la copia congelata resta invariata.");
      revalidator.revalidate();
    } catch (cause) {
      setError(errorMessage(cause));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="content-grid">
      <Link to="/" className="back-link">
        Tutte le gare
      </Link>
      {(!draft || !estimate) && (
        <ErrorMessage>
          Accedi per visualizzare e preparare questa gara.
        </ErrorMessage>
      )}
      {draft && estimate && (
        <>
          <section className="section-heading">
            <div>
              <h1>Preparazione gara</h1>
              <p className="quiet-copy">Revisione del piano {draft.revision}</p>
            </div>
            <span
              className={
                estimate.isAvailable
                  ? "status status-final"
                  : "status status-warning"
              }
            >
              {estimate.isAvailable ? "Stima disponibile" : "Stima incompleta"}
            </span>
          </section>
          {draft.frozenSnapshotId && (
            <div className="notice notice-info">
              <p>
                Questo piano è congelato. Puoi riaprire una nuova bozza senza
                modificare la cronologia congelata.
              </p>
              <button
                type="button"
                className="button button-secondary"
                disabled={busy}
                onClick={() => void reopenDraft()}
              >
                Riapri bozza
              </button>
            </div>
          )}
          <Link
            className="button button-secondary identity-review-link"
            to={`/matches/${encodeURIComponent(matchId ?? "")}/identities`}
          >
            Rivedi identità dei risultati
          </Link>
          {matchId && (
            <ResultWorkspace matchId={matchId} results={data.results} />
          )}
          {!estimate.isAvailable && (
            <div className="notice notice-warning">
              <strong>Stima non ancora verificabile.</strong>
              <ul>
                {estimate.diagnostics.map((item) => (
                  <li key={item}>{item}</li>
                ))}
              </ul>
              Il congelamento salva una copia immutabile del piano corrente; non
              trasforma una stima incompleta in un risultato.
            </div>
          )}
          <div className="plan-layout">
            <section className="panel">
              <div className="panel-heading">
                <div>
                  <h2>Definisci il piano</h2>
                </div>
              </div>
              <form
                className="form-stack"
                onSubmit={(event) => void saveAndFreeze(event)}
              >
                <div className="form-columns">
                  <label>
                    Identità
                    <select
                      name="identityId"
                      defaultValue={
                        draft.identityId ?? profile?.identityId ?? ""
                      }
                      required
                    >
                      <option value="">Seleziona identità</option>
                      {(data.identities ?? []).map((identity) => (
                        <option key={identity.id} value={identity.id}>
                          {identity.aliases[0] ?? identity.normalizedName}
                        </option>
                      ))}
                    </select>
                  </label>
                  <label>
                    Divisione
                    <DivisionSelect
                      options={data.referenceData.divisions}
                      value={draft.division ?? profile?.division}
                      required
                    />
                  </label>
                </div>
                <div className="form-columns">
                  <label>
                    Beta <ContextHelp id="beta-help">Controlla quanto il modello pesa la prestazione storica nella stima dell’hit factor.</ContextHelp>
                    <input
                      name="beta"
                      type="number"
                      min="0.1"
                      max="3"
                      step="0.01"
                      defaultValue={draft.beta}
                    />
                  </label>
                  <label>
                    Deriva % <ContextHelp id="drift-help">Rappresenta la variazione attesa della prestazione nel tempo: 0,04 corrisponde al 4%.</ContextHelp>
                    <input
                      name="driftPercent"
                      type="number"
                      min="0"
                      max="0.5"
                      step="0.005"
                      defaultValue={draft.driftPercent}
                    />
                  </label>
                  <label>
                    Variabilità
                    <input
                      name="ownVariability"
                      type="number"
                      min="0"
                      max="0.5"
                      step="0.005"
                      defaultValue={draft.ownVariability}
                    />
                  </label>
                  <label>
                    Penalità distanza
                    <input
                      name="distancePenalty"
                      type="number"
                      min="0"
                      max="2"
                      step="0.05"
                      defaultValue={draft.distancePenalty}
                    />
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
                    defaultValue={draft.performance}
                  />
                </label>
                <p className="field-hint">
                  Quota attesa dei punti massimi usata per convertire i punti
                  persi in secondi equivalenti. 0,935 = 93,5%; non è ranking o
                  confidenza.
                </p>
                <label className="checkbox-label">
                  <input
                    name="applyAfternoon"
                    type="checkbox"
                    defaultChecked={draft.applyAfternoon}
                  />{" "}
                  Considera l'effetto pomeridiano
                </label>
                <label className="checkbox-label">
                  <input
                    name="applyOneHand"
                    type="checkbox"
                    defaultChecked={draft.applyOneHand}
                  />{" "}
                  Considera gli stage a una mano
                </label>
                <fieldset className="stage-options">
                  <legend>Decisioni per stage</legend>
                  {estimate.stages.map((stage) => (
                    <div className="stage-option" key={stage.stageId}>
                      <label className="checkbox-label">
                        <input
                          name={`exclude:${stage.stageId}`}
                          type="checkbox"
                          defaultChecked={draft.excludedStageIds.includes(
                            stage.stageId,
                          )}
                        />
                        Escludi stage {stage.stageNumber} dai calcoli
                      </label>
                      <label>
                        Strategia / nota
                        <textarea
                          name={`strategy:${stage.stageId}`}
                          rows={2}
                          maxLength={1000}
                          defaultValue={
                            draft.strategyByStage[stage.stageId] ?? ""
                          }
                        />
                      </label>
                    </div>
                  ))}
                </fieldset>
                {error && <ErrorMessage>{error}</ErrorMessage>}
                {status && (
                  <p className="notice notice-success" role="status">
                    {status}
                  </p>
                )}
                <div className="action-row">
                  <button
                    className="button button-secondary"
                    name="intent"
                    value="save"
                    disabled={busy}
                  >
                    {busy ? "Salvataggio…" : "Salva bozza"}
                  </button>
                  <button
                    className="button button-primary"
                    name="intent"
                    value="freeze"
                    disabled={busy}
                  >
                    {busy ? "Congelamento…" : "Salva e congela il piano"}
                  </button>
                </div>
              </form>
            </section>
            <aside className="estimate-panel">
              <h2>Stima per stage</h2>
              <p className="quiet-copy">Modello {estimate.engineVersion}</p>
              <p className="quiet-copy">
                Dispersione: {estimate.dispersionConvention}
              </p>
              {estimate.stages.length === 0 ? (
                <p className="muted-copy">
                  Nessun dato di stage disponibile per questa stima.
                </p>
              ) : (
                estimate.stages.map((stage) => (
                  <article className="stage-estimate" key={stage.stageId}>
                    <div>
                      <span>
                        STAGE {String(stage.stageNumber).padStart(2, "0")}
                      </span>
                      <strong>
                        {stage.center === null ? "—" : stage.center.toFixed(2)}
                      </strong>
                    </div>
                    <p>
                      {stage.minimumRounds} colpi minimi
                      {stage.rhythmSymbol
                        ? ` · ritmo ${stage.rhythmSymbol}`
                        : ""}
                    </p>
                    <small>
                      Intervallo {stage.lower?.toFixed(2) ?? "—"}–
                      {stage.upper?.toFixed(2) ?? "—"}
                    </small>
                  </article>
                ))
              )}
            </aside>
          </div>
        </>
      )}
    </div>
  );
}
