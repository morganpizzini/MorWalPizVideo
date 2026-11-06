import {
  useState,
  type FormEvent
} from "react";
import {
  Link,
  useLoaderData,
  useRouteLoaderData,
  useParams
} from "react-router-dom";
import {
  useEffect
} from "react";

import {
  freezerApi,
  FreezerApiError,
  type MatchSummary,
  type ParameterAdoption,
  type ParameterBacktestReport,
  type ParameterValues,
  type Snapshot,
  type SnapshotSelection,
  type VerificationReport
} from "../freezerApi";


import {
  ContextHelp,
  ErrorMessage,
  errorMessage,
  formatDate
} from "./shared";
import type { RootData } from "./shared";

interface VerificationLoaderData {
  snapshot: Snapshot;
  match: MatchSummary | null;
}

export function VerificationPage() {
  const data = useLoaderData() as VerificationLoaderData | null;
  const offline =
    (useRouteLoaderData("root") as RootData | undefined)?.offline ?? false;
  const { snapshotId } = useParams();
  const [report, setReport] = useState<VerificationReport | null>(null);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [backtestError, setBacktestError] = useState("");
  const [backtestBusy, setBacktestBusy] = useState(false);
  const [backtest, setBacktest] = useState<ParameterBacktestReport | null>(
    null,
  );
  const [snapshots, setSnapshots] = useState<SnapshotSelection[]>([]);
  const [matches, setMatches] = useState<MatchSummary[]>([]);
  const [selectionLoading, setSelectionLoading] = useState(true);
  const [selectionError, setSelectionError] = useState("");
  const [selectedSnapshotIds, setSelectedSnapshotIds] = useState<string[]>([]);
  const [parameters, setParameters] = useState<ParameterValues>({
    beta: 1.2,
    driftPercent: 0.04,
    ownVariability: 0.105,
    distancePenalty: 0.25,
  });
  const [adoptionRevision, setAdoptionRevision] = useState(0);
  const [adoptionConfirmed, setAdoptionConfirmed] = useState(false);
  const [adoptionBusy, setAdoptionBusy] = useState(false);
  const [adoptionMessage, setAdoptionMessage] = useState("");

  function updateParameters(update: Partial<ParameterValues>) {
    setParameters((current) => ({ ...current, ...update }));
    setBacktestError("");
    setBacktest(null);
    setAdoptionConfirmed(false);
    setAdoptionMessage("");
  }

  useEffect(() => {
    let active = true;
    if (offline) {
      setSelectionLoading(false);
      return () => {
        active = false;
      };
    }
    async function loadBacktestOptions() {
      try {
        const [listedSnapshots, listedMatches, adoption] = await Promise.all([
          freezerApi.getSnapshots(),
          freezerApi.getMatches(),
          freezerApi.getLatestParameterAdoption().catch((cause) => {
            if (cause instanceof FreezerApiError && cause.status === 404)
              return null;
            throw cause;
          }),
        ]);
        if (!active) return;
        const availableSnapshots = listedSnapshots;
        setSnapshots(availableSnapshots);
        setMatches(listedMatches);
        if (adoption) {
          setParameters(adoption.parameters);
          setAdoptionRevision(adoption.revision);
        }
        const matchById = new Map(listedMatches.map((item) => [item.id, item]));
        if (data?.match) matchById.set(data.match.id, data.match);
        setSelectedSnapshotIds(
          availableSnapshots
            .filter((item) => {
              const match = matchById.get(item.matchId);
              return (
                match?.isFinal &&
                match.canVerify &&
                match.activeRevisionId !== item.matchRevisionId
              );
            })
            .filter((item) => item.id === data?.snapshot.id)
            .map((item) => item.id),
        );
      } catch (cause) {
        if (active) setSelectionError(errorMessage(cause));
      } finally {
        if (active) setSelectionLoading(false);
      }
    }
    void loadBacktestOptions();
    return () => {
      active = false;
    };
  }, [data?.snapshot.id, data?.match, offline]);

  async function verify() {
    if (offline || !data?.match?.isFinal || !data.match.canVerify) return;
    setError("");
    setBusy(true);
    try {
      setReport(
        await freezerApi.verifySnapshot(
          data.snapshot.id,
          data.match.activeRevisionId,
        ),
      );
    } catch (cause) {
      setError(errorMessage(cause));
    } finally {
      setBusy(false);
    }
  }

  async function runBacktest(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (offline || !selectedEvaluations.length) return;
    setBacktestError("");
    setAdoptionMessage("");
    setBacktestBusy(true);
    setBacktest(null);
    setBacktestError("");
    setAdoptionConfirmed(false);
    try {
      setBacktest(
        await freezerApi.backtestParameters(parameters, selectedEvaluations),
      );
      setAdoptionConfirmed(false);
    } catch (cause) {
      setBacktestError(errorMessage(cause));
    } finally {
      setBacktestBusy(false);
    }
  }

  async function adoptBacktest() {
    if (offline || !backtest || !canAdoptBacktest || !adoptionConfirmed) return;
    setBacktestError("");
    setAdoptionMessage("");
    setAdoptionBusy(true);
    try {
      const adoption: ParameterAdoption = await freezerApi.adoptParameters(
        backtest.id,
        adoptionRevision,
        adoptionConfirmed,
      );
      setAdoptionRevision(adoption.revision);
      setParameters(adoption.parameters);
      setAdoptionMessage(
        `Parametri adottati alla revisione ${adoption.revision}. Si applicheranno alle nuove bozze; i piani già creati non cambiano.`,
      );
    } catch (cause) {
      setBacktestError(errorMessage(cause));
    } finally {
      setAdoptionBusy(false);
    }
  }

  if (!data) {
    return (
      <section className="form-panel">
        <h1>Verifica non disponibile.</h1>
        <p className="lede">
          Accedi per confrontare un piano congelato con i risultati finali.
        </p>
      </section>
    );
  }

  const match = data.match;
  const matchById = new Map(matches.map((item) => [item.id, item]));
  if (match) matchById.set(match.id, match);
  const eligibleSnapshots = snapshots.filter((item) => {
    const candidateMatch = matchById.get(item.matchId);
    return (
      candidateMatch?.isFinal &&
      candidateMatch.canVerify &&
      candidateMatch.activeRevisionId !== item.matchRevisionId
    );
  });
  const selectedEvaluations = eligibleSnapshots
    .filter((item) => selectedSnapshotIds.includes(item.id))
    .map((item) => ({
      snapshotId: item.id,
      finalRevisionId: matchById.get(item.matchId)!.activeRevisionId,
    }));
  const canAdoptBacktest = Boolean(
    backtest &&
    backtest.engineVersion &&
    backtest.evaluations.length > 0 &&
    backtest.evaluations.every(
      (item) => item.status === "evaluated" && item.verifiableStageCount > 0,
    ) &&
    backtest.verifiableStageCount > 0,
  );
  const numberFormat = new Intl.NumberFormat("it-IT", {
    maximumFractionDigits: 4,
  });
  const percent = (value: number | null) =>
    value === null ? "—" : `${numberFormat.format(value * 100)}%`;
  const intervalStatus = (value: boolean | null) =>
    value === null
      ? "Non verificabile"
      : value
        ? "Dentro l’intervallo"
        : "Fuori intervallo";
  const stageGroupName = (value: string) =>
    value === "short"
      ? "Brevi"
      : value === "medium"
        ? "Medie"
        : value === "long"
          ? "Lunghe"
          : value;
  const stageGroupingConvention =
    report?.stageGroupingConvention ===
    "minimum rounds: short=1-12, medium=13-24, long=25+"
      ? "round minimi: brevi 1–12, medie 13–24, lunghe 25 o più"
      : report?.stageGroupingConvention || "non disponibile";

  return (
    <div className="content-grid verification-page">
      <Link
        to={`/field/${encodeURIComponent(snapshotId ?? data.snapshot.id)}`}
        className="back-link"
      >
        Piano congelato
      </Link>
      <section className="section-heading">
        <div>
          <h1>Verifica finale</h1>
          <p className="lede">
            Il confronto è separato dal piano congelato e non modifica stime o
            note.
          </p>
        </div>
      </section>
      <section className="panel verification-panel">
        <h2>{match?.name ?? "Gara non disponibile"}</h2>
        <p className="quiet-copy">
          Piano congelato il {formatDate(data.snapshot.createdAtUtc)} ·{" "}
          {data.snapshot.stages.length} stage
        </p>
        {!match && (
          <p className="notice notice-warning">
            Non è disponibile una revisione gara associata a questo piano.
          </p>
        )}
        {match && !match.isFinal && (
          <p className="notice notice-warning">
            La revisione corrente non è finale. Pubblica i risultati ufficiali
            finali prima della verifica.
          </p>
        )}
        {match?.isFinal && !match.canVerify && (
          <p className="notice notice-warning">
            La revisione finale è condivisa, ma non risulta pubblicata dal tuo
            account. La verifica è consentita solo sulla revisione finale di cui
            possiedi l’import.
          </p>
        )}
        {match?.isFinal && match.canVerify && !report && (
          <>
            <p className="quiet-copy">
              Revisione ufficiale finale {match.revisionNumber} pronta per il
              confronto.
            </p>
            <button
              type="button"
              className="button button-primary"
              disabled={busy || offline}
              onClick={() => void verify()}
            >
              {busy ? "Confronto…" : "Calcola verifica"}
            </button>
          </>
        )}
        {error && <ErrorMessage>{error}</ErrorMessage>}
      </section>
      {report && (
        <section className="panel verification-report" aria-live="polite">
          <div className="panel-heading">
            <div>
              <h2>Risultato della verifica</h2>
              <p className="quiet-copy">
                Report {report.id} · revisione finale associata al piano.
              </p>
            </div>
            <span className="status status-final">Salvato</span>
          </div>
          <dl className="verification-stats">
            <div>
              <dt>Stage verificabili</dt>
              <dd>{report.verifiableStageCount}</dd>
            </div>
            <div>
              <dt>Stage mancanti</dt>
              <dd>{report.missingStageCount}</dd>
            </div>
            <div>
              <dt>Stage DQ</dt>
              <dd>{report.disqualifiedStageCount}</dd>
            </div>
            <div>
              <dt>Stage azzerati</dt>
              <dd>{report.zeroedStageCount}</dd>
            </div>
            <div>
              <dt>Errore relativo medio</dt>
              <dd>{percent(report.meanRelativeError)}</dd>
            </div>
            <div>
              <dt>Dispersione errore</dt>
              <dd>{percent(report.errorSampleStandardDeviation)}</dd>
            </div>
            <div>
              <dt>Copertura intervallo</dt>
              <dd>
                {report.intervalCoverageCount} / {report.verifiableStageCount}
              </dd>
            </div>
            <div>
              <dt>Beta osservato</dt>
              <dd>
                {report.observedBeta === null
                  ? "—"
                  : numberFormat.format(report.observedBeta)}
              </dd>
            </div>
            <div>
              <dt>Confronti informatore-stage</dt>
              <dd>{report.informantDriftComparisonCount}</dd>
            </div>
            <div>
              <dt>Confronti informatore-stage mancanti</dt>
              <dd>{report.informantDriftMissingStageCount}</dd>
            </div>
            <div>
              <dt>Confronti informatore-stage DQ</dt>
              <dd>{report.informantDriftDisqualifiedStageCount}</dd>
            </div>
            <div>
              <dt>Confronti informatore-stage azzerati</dt>
              <dd>{report.informantDriftZeroedStageCount}</dd>
            </div>
            <div>
              <dt>Confronti informatore-stage non verificabili</dt>
              <dd>{report.informantDriftUnverifiableStageCount}</dd>
            </div>
            <div>
              <dt>Errore medio degli informatori</dt>
              <dd>{percent(report.informantDriftMeanRelativeError)}</dd>
            </div>
            <div>
              <dt>Dispersione errore informatori</dt>
              <dd>
                {percent(report.informantDriftErrorSampleStandardDeviation)}
              </dd>
            </div>
          </dl>
          {report.informantDriftComparisonCount === 0 && (
            <p className="quiet-copy">
              Confronto con gli informatori non disponibile per questa verifica.
            </p>
          )}
          {report.diagnostics.length > 0 && (
            <ul className="diagnostic-list">
              {report.diagnostics.map((item, index) => (
                <li key={`${index}-${item}`}>{item}</li>
              ))}
            </ul>
          )}
          <section
            className="verification-groups"
            aria-labelledby="verification-groups-heading"
          >
            <div>
              <h3 id="verification-groups-heading">
                Risultati per durata stage
              </h3>
              <p className="quiet-copy">Criterio: {stageGroupingConvention}.</p>
            </div>
            {report.groups.length === 0 ? (
              <p className="quiet-copy">
                Non ci sono stage raggruppabili in questa verifica.
              </p>
            ) : (
              <div className="pdf-table-scroll">
                <table>
                  <thead>
                    <tr>
                      <th>Gruppo</th>
                      <th>Stage</th>
                      <th>Verificabili</th>
                      <th>Mancanti</th>
                      <th>DQ</th>
                      <th>Azzerati</th>
                      <th>Non verificabili</th>
                      <th>Errore medio</th>
                      <th>Dispersione</th>
                      <th>Copertura intervallo</th>
                    </tr>
                  </thead>
                  <tbody>
                    {report.groups.map((group) => (
                      <tr key={group.stageGroup}>
                        <th scope="row">{stageGroupName(group.stageGroup)}</th>
                        <td>{group.stageCount}</td>
                        <td>{group.verifiableStageCount}</td>
                        <td>{group.missingStageCount}</td>
                        <td>{group.disqualifiedStageCount}</td>
                        <td>{group.zeroedStageCount}</td>
                        <td>{group.unverifiableStageCount}</td>
                        <td>{percent(group.meanRelativeError)}</td>
                        <td>{percent(group.errorSampleStandardDeviation)}</td>
                        <td>
                          {group.intervalCoverageCount} /{" "}
                          {group.verifiableStageCount} (
                          {percent(group.intervalCoverageRate)})
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
          <div className="pdf-table-scroll">
            <table>
              <thead>
                <tr>
                  <th>Stage</th>
                  <th>Gruppo</th>
                  <th>Esito</th>
                  <th>HF reale</th>
                  <th>Stima</th>
                  <th>Intervallo</th>
                  <th>Errore relativo</th>
                  <th>Copertura</th>
                </tr>
              </thead>
              <tbody>
                {report.stages.map((stage) => (
                  <tr key={stage.stageId}>
                    <td>{stage.stageNumber}</td>
                    <td>
                      {stage.stageGroup
                        ? stageGroupName(stage.stageGroup)
                        : "Non raggruppato"}
                    </td>
                    <td>{stage.status}</td>
                    <td>
                      {stage.actualHitFactor === null
                        ? "—"
                        : numberFormat.format(stage.actualHitFactor)}
                    </td>
                    <td>
                      {stage.forecastCenter === null
                        ? "—"
                        : numberFormat.format(stage.forecastCenter)}
                    </td>
                    <td>
                      {stage.forecastLower === null ||
                      stage.forecastUpper === null
                        ? "—"
                        : `${numberFormat.format(stage.forecastLower)}–${numberFormat.format(stage.forecastUpper)}`}
                    </td>
                    <td>{percent(stage.relativeError)}</td>
                    <td>{intervalStatus(stage.isWithinPredictionInterval)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}
      <section className="panel parameter-backtest">
        <div className="panel-heading">
          <div>
            <h2>Confronta parametri</h2>
            <p className="quiet-copy">
              Ogni stima usa solo gli input congelati di quel piano e la
              revisione finale selezionata.
            </p>
          </div>
          <span className="status">{selectedEvaluations.length} gare</span>
        </div>
        {offline && (
          <p className="notice notice-warning">
            Verifica, backtest e adozione richiedono una connessione attiva. Le
            stime e i piani congelati restano invariati.
          </p>
        )}
        {selectionLoading && (
          <p className="quiet-copy" role="status">
            Caricamento dei piani finali disponibili…
          </p>
        )}
        {selectionError && <ErrorMessage>{selectionError}</ErrorMessage>}
        {!offline &&
          !selectionLoading &&
          !selectionError &&
          eligibleSnapshots.length === 0 && (
            <p className="notice notice-info">
              Non ci sono piani con risultati finali pubblicati dal tuo account
              e distinti dalla revisione congelata.
            </p>
          )}
        {!offline && !selectionLoading && eligibleSnapshots.length > 0 && (
          <form
            className="form-stack"
            onSubmit={(event) => void runBacktest(event)}
          >
            <fieldset className="backtest-selection">
              <legend>Piani da confrontare</legend>
              {eligibleSnapshots.map((item) => {
                const candidateMatch = matchById.get(item.matchId)!;
                return (
                  <label
                    className="checkbox-label backtest-option"
                    key={item.id}
                  >
                    <input
                      type="checkbox"
                      checked={selectedSnapshotIds.includes(item.id)}
                      onChange={(event) => {
                        const checked = event.currentTarget.checked;
                        setSelectedSnapshotIds((current) =>
                          checked
                            ? [...current, item.id]
                            : current.filter((id) => id !== item.id),
                        );
                        setBacktest(null);
                        setAdoptionConfirmed(false);
                        setAdoptionMessage("");
                      }}
                    />
                    <span>
                      <strong>{candidateMatch.name}</strong>
                      <small>
                        Piano {formatDate(item.createdAtUtc)} · revisione finale{" "}
                        {candidateMatch.revisionNumber}
                      </small>
                    </span>
                  </label>
                );
              })}
            </fieldset>
            <div className="form-columns parameter-fields">
              <label>
                Beta <ContextHelp id="backtest-beta-help">Parametro di sensibilità della stima: valori maggiori danno più peso alla prestazione osservata.</ContextHelp>
                <input
                  type="number"
                  min="0.1"
                  max="5"
                  step="0.01"
                  value={parameters.beta}
                  onChange={(event) =>
                    updateParameters({
                      beta: Number(event.currentTarget.value),
                    })
                  }
                  required
                />
              </label>
              <label>
                Deriva (0,04 = 4%) <ContextHelp id="backtest-drift-help">Variazione percentuale prevista tra gare successive; inserisci 0,04 per il 4%.</ContextHelp>
                <input
                  type="number"
                  min="0"
                  max="0.5"
                  step="0.005"
                  value={parameters.driftPercent}
                  onChange={(event) =>
                    updateParameters({
                      driftPercent: Number(event.currentTarget.value),
                    })
                  }
                  required
                />
              </label>
              <label>
                Variabilità personale <ContextHelp id="backtest-variability-help">Incertezza naturale della prestazione dell’atleta, espressa come quota.</ContextHelp>
                <input
                  type="number"
                  min="0.001"
                  max="1"
                  step="0.001"
                  value={parameters.ownVariability}
                  onChange={(event) =>
                    updateParameters({
                      ownVariability: Number(event.currentTarget.value),
                    })
                  }
                  required
                />
              </label>
              <label>
                Penalità distanza <ContextHelp id="backtest-distance-help">Riduzione stimata per la distanza, applicata solo quando il parametro è confermato.</ContextHelp>
                <input
                  type="number"
                  min="0"
                  max="2"
                  step="0.01"
                  value={parameters.distancePenalty}
                  onChange={(event) =>
                    updateParameters({
                      distancePenalty: Number(event.currentTarget.value),
                    })
                  }
                  required
                />
              </label>
            </div>
            <button
              type="submit"
              className="button button-primary"
              disabled={backtestBusy || selectedEvaluations.length === 0}
            >
              {backtestBusy ? "Confronto…" : "Esegui backtest"}
            </button>
          </form>
        )}
        {backtestError && <ErrorMessage>{backtestError}</ErrorMessage>}
        {adoptionMessage && (
          <p className="notice notice-success" role="status">
            {adoptionMessage}
          </p>
        )}
        {backtest && (
          <div className="parameter-backtest-report" aria-live="polite">
            <h3>Esito del backtest</h3>
            <p className="quiet-copy">
              Parametri testati: beta{" "}
              {numberFormat.format(backtest.parameters.beta)} · deriva{" "}
              {percent(backtest.parameters.driftPercent)} · variabilità{" "}
              {numberFormat.format(backtest.parameters.ownVariability)} ·
              distanza{" "}
              {numberFormat.format(backtest.parameters.distancePenalty)} ·
              versione {backtest.parameterVersion}
            </p>
            <dl className="verification-stats">
              <div>
                <dt>Stage verificabili</dt>
                <dd>{backtest.verifiableStageCount}</dd>
              </div>
              <div>
                <dt>Errore relativo medio</dt>
                <dd>{percent(backtest.meanRelativeError)}</dd>
              </div>
              <div>
                <dt>Dispersione errore</dt>
                <dd>{percent(backtest.errorSampleStandardDeviation)}</dd>
              </div>
              <div>
                <dt>Copertura intervallo</dt>
                <dd>
                  {backtest.intervalCoverageCount} /{" "}
                  {backtest.verifiableStageCount}
                </dd>
              </div>
            </dl>
            {backtest.diagnostics.length > 0 && (
              <ul className="diagnostic-list">
                {backtest.diagnostics.map((item, index) => (
                  <li key={`${index}-${item}`}>{item}</li>
                ))}
              </ul>
            )}
            {backtest.evaluations.map((evaluation) => (
              <details
                className="backtest-evaluation"
                key={`${evaluation.snapshotId}-${evaluation.finalRevisionId}`}
              >
                <summary>
                  {matchById.get(evaluation.matchId)?.name ??
                    evaluation.matchId}{" "}
                  · {evaluation.verifiableStageCount} stage verificabili ·{" "}
                  {evaluation.status}
                </summary>
                {evaluation.diagnostics.length > 0 && (
                  <ul className="diagnostic-list">
                    {evaluation.diagnostics.map((item, index) => (
                      <li key={`${index}-${item}`}>{item}</li>
                    ))}
                  </ul>
                )}
                <div className="pdf-table-scroll">
                  <table>
                    <thead>
                      <tr>
                        <th>Stage</th>
                        <th>Esito</th>
                        <th>HF reale</th>
                        <th>Stima candidata</th>
                        <th>Errore relativo</th>
                      </tr>
                    </thead>
                    <tbody>
                      {evaluation.stages.map((stage) => (
                        <tr key={stage.stageId}>
                          <td>{stage.stageNumber}</td>
                          <td>{stage.status}</td>
                          <td>
                            {stage.actualHitFactor === null
                              ? "—"
                              : numberFormat.format(stage.actualHitFactor)}
                          </td>
                          <td>
                            {stage.forecastCenter === null
                              ? "—"
                              : numberFormat.format(stage.forecastCenter)}
                          </td>
                          <td>{percent(stage.relativeError)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </details>
            ))}
            <div className="adoption-confirmation">
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  checked={adoptionConfirmed}
                  onChange={(event) =>
                    setAdoptionConfirmed(event.currentTarget.checked)
                  }
                />{" "}
                Confermo di voler usare questi parametri nelle nuove bozze del
                mio account.
              </label>
              <button
                type="button"
                className="button button-secondary"
                disabled={
                  !canAdoptBacktest || !adoptionConfirmed || adoptionBusy
                }
                onClick={() => void adoptBacktest()}
              >
                {adoptionBusy ? "Salvataggio…" : "Adotta per le nuove bozze"}
              </button>
              {!canAdoptBacktest && (
                <p className="quiet-copy">
                  L’adozione richiede risultati verificabili per tutti i piani
                  selezionati.
                </p>
              )}
            </div>
          </div>
        )}
      </section>
    </div>
  );
}
