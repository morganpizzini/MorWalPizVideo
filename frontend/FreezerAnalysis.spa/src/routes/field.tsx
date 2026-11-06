import {
  useState,
  type FormEvent
} from "react";
import {
  Link,
  useLoaderData,
  useParams
} from "react-router-dom";
import {
  useEffect
} from "react";

import {
  freezerApi,
  type FieldEvent,
  type FieldEventInput,
  type SnapshotReplay
} from "../freezerApi";
import {
  clearAccountOfflineData,
  enqueueFieldEvent,
  getSnapshot,
  queuedFieldEvents,
  removeQueuedFieldEvent
} from "../offlineStore";

import {
  rootData,
  optionalPositiveInteger,
  localDateTime,
  parseEventKind,
  ErrorMessage,
  errorMessage,
  formatDate,
  newOperationId
} from "./shared";

interface FieldLoaderData {
  snapshot: import("../freezerApi").Snapshot;
  events: FieldEvent[];
  offline: boolean;
}

export function FieldPage() {
  const {
    snapshot,
    events: initialEvents,
    offline: loaderOffline,
  } = useLoaderData() as FieldLoaderData;
  const { snapshotId } = useParams();
  const { session, offline: accountOffline } = rootData();
  const accountId = session.account?.id;
  const [events, setEvents] = useState(initialEvents);
  const [pendingCount, setPendingCount] = useState(0);
  const [isOffline, setIsOffline] = useState(loaderOffline || accountOffline);
  const [error, setError] = useState("");
  const [status, setStatus] = useState("");
  const [eventKind, setEventKind] =
    useState<FieldEventInput["kind"]>("reconnaissance");
  const [downloading, setDownloading] = useState(false);
  const [downloadingSide, setDownloadingSide] = useState<"front" | "back" | null>(null);
  const [replaying, setReplaying] = useState(false);
  const [replay, setReplay] = useState<SnapshotReplay | null>(null);

  useEffect(() => {
    void refreshPending()
      .then(async () => {
        if (!accountId || !snapshotId) return;
        const queued = await queuedFieldEvents(accountId);
        const localEvents: FieldEvent[] = queued
          .filter((item) => item.snapshotId === snapshotId)
          .map((item) => ({
            id: `queued:${item.event.clientOperationId}`,
            snapshotId,
            sequence: 0,
            kind: item.event.kind,
            content: item.event.content,
            serverReceivedAtUtc: "",
            observedAtLocal: item.event.observedAtLocal,
            observedOffsetMinutes: item.event.observedOffsetMinutes,
            startStage: item.event.startStage ?? null,
            startTurn: item.event.startTurn ?? null,
            squadSize: item.event.squadSize ?? null,
            displayOrder: item.event.displayOrder ?? null,
          }));
        setEvents((previous) => [
          ...previous.filter((item) => !item.id.startsWith("queued:")),
          ...localEvents,
        ]);
        if (
          !loaderOffline &&
          !accountOffline &&
          window.navigator.onLine &&
          queued.length > 0
        )
          void synchronize();
      })
      .catch((cause) => setError(errorMessage(cause)));
    const handleOnline = () => {
      setIsOffline(false);
      void synchronize();
    };
    window.addEventListener("online", handleOnline);
    return () => window.removeEventListener("online", handleOnline);
  }, [accountId, snapshotId, loaderOffline, accountOffline]);

  async function refreshPending() {
    if (!accountId) return;
    setPendingCount((await queuedFieldEvents(accountId)).length);
  }

  async function submitEvent(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formElement = event.currentTarget;
    if (!accountId || !snapshotId) {
      setError("Account o piano non disponibile.");
      return;
    }
    setError("");
    setStatus("");
    let payload: FieldEventInput | null = null;
    try {
      const form = new FormData(formElement);
      const now = new Date();
      const startStage = optionalPositiveInteger(form.get("startStage"));
      const startTurn = optionalPositiveInteger(form.get("startTurn"));
      const squadSize = optionalPositiveInteger(form.get("squadSize"));
      const displayOrderInput = String(form.get("displayOrder") ?? "")
        .split(",")
        .map((value) => value.trim())
        .filter(Boolean);
      const displayOrder = displayOrderInput.map((value) => {
        const stageNumber = Number(value);
        const stage = snapshot.stages.find(
          (item) => item.stageNumber === stageNumber,
        );
        if (!Number.isInteger(stageNumber) || !stage) {
          throw new Error(
            "L’ordine deve contenere numeri di stage presenti nel piano.",
          );
        }
        return stage.stageId;
      });
      if (form.get("kind") === "turn" && displayOrder.length === 0 && startStage) {
        const ordered = [...snapshot.stages].sort((a, b) => a.stageNumber - b.stageNumber);
        const startIndex = ordered.findIndex((stage) => stage.stageNumber === startStage);
        if (startIndex >= 0)
          displayOrder.push(
            ...ordered.slice(startIndex).map((stage) => stage.stageId),
            ...ordered.slice(0, startIndex).map((stage) => stage.stageId),
          );
      }
      payload = {
        clientOperationId: newOperationId(),
        kind: parseEventKind(form.get("kind")),
        content: String(form.get("content") ?? ""),
        observedAtLocal: localDateTime(now),
        observedOffsetMinutes: -now.getTimezoneOffset(),
        ...(form.get("kind") === "turn"
          ? { startStage, startTurn, squadSize, displayOrder }
          : {}),
      };
      const saved = await freezerApi.appendFieldEvent(snapshotId, payload);
      setEvents((previous) => [...previous, saved]);
      setStatus("Nota sincronizzata.");
      formElement.reset();
    } catch (cause) {
      if (!(cause instanceof TypeError) || !payload) {
        setError(errorMessage(cause));
        return;
      }
      try {
        await enqueueFieldEvent(accountId, snapshotId, payload);
        setPendingCount((count) => count + 1);
        setIsOffline(true);
        setStatus(
          "Nota salvata sul dispositivo; verrà sincronizzata al ritorno della connessione.",
        );
        formElement.reset();
      } catch (storageError) {
        setError(errorMessage(storageError));
      }
    }
  }

  async function synchronize() {
    if (!accountId) return;
    setError("");
    let queued: Awaited<ReturnType<typeof queuedFieldEvents>>;
    try {
      const session = await freezerApi.getSession();
      if (!session.authenticated || !session.account) {
        setError(
          "Accedi di nuovo con l’account che ha salvato le note; la coda locale è conservata.",
        );
        return;
      }
      if (session.account.id !== accountId) {
        await clearAccountOfflineData(accountId);
        setPendingCount(0);
        setError(
          "La sessione attiva non corrisponde all’account che ha salvato le note. Accedi con l’account corretto prima di sincronizzare.",
        );
        return;
      }
      queued = await queuedFieldEvents(accountId);
    } catch (cause) {
      setError(errorMessage(cause));
      return;
    }
    const verifiedSnapshots = new Set<string>();
    for (const item of queued) {
      try {
        if (!verifiedSnapshots.has(item.snapshotId)) {
          await freezerApi.getSnapshot(item.snapshotId);
          verifiedSnapshots.add(item.snapshotId);
        }
        const saved = await freezerApi.appendFieldEvent(
          item.snapshotId,
          item.event,
        );
        await removeQueuedFieldEvent(accountId, item.event.clientOperationId);
        if (item.snapshotId === snapshotId) {
          setEvents((previous) => [
            ...previous.filter(
              (existing) =>
                existing.id !== `queued:${item.event.clientOperationId}`,
            ),
            ...(previous.some((existing) => existing.id === saved.id)
              ? []
              : [saved]),
          ]);
        }
      } catch (cause) {
        setError(errorMessage(cause));
        return;
      }
    }
    setPendingCount(0);
    setIsOffline(false);
    setStatus("Tutte le note in coda sono sincronizzate.");
  }

  async function refreshOfflineSnapshot() {
    if (!accountId || !snapshotId) return;
    const cached = await getSnapshot(accountId, snapshotId);
    if (!cached) {
      setError("Non è disponibile una copia locale di questo piano.");
      return;
    }
    setStatus("Piano locale verificato.");
  }

  async function downloadDocuments() {
    if (!snapshotId) return;
    setDownloading(true);
    setError("");
    try {
      const archive = await freezerApi.createArtifactPackage(snapshotId);
      const objectUrl = URL.createObjectURL(archive);
      const anchor = document.createElement("a");
      anchor.href = objectUrl;
      anchor.download = `freezer-card-${snapshotId}.zip`;
      anchor.click();
      URL.revokeObjectURL(objectUrl);
      setStatus("Pacchetto dei documenti scaricato.");
    } catch (cause) {
      setError(errorMessage(cause));
    } finally {
      setDownloading(false);
    }

  }

  async function downloadImage(side: "front" | "back") {
    if (!snapshotId) return;
    setDownloadingSide(side);
    setError("");
    try {
      const image = await freezerApi.downloadArtifactImage(snapshotId, side);
      const objectUrl = URL.createObjectURL(image);
      const anchor = document.createElement("a");
      anchor.href = objectUrl;
      anchor.download = `freezer-card-${snapshotId}-phone-${side}.png`;
      anchor.click();
      URL.revokeObjectURL(objectUrl);
      setStatus(`Immagine ${side === "front" ? "frontale" : "posteriore"} scaricata.`);
    } catch (cause) {
      setError(errorMessage(cause));
    } finally {
      setDownloadingSide(null);
    }
  }

  async function replaySnapshot() {
    if (!snapshotId) return;
    setError("");
    setReplay(null);
    setReplaying(true);
    try {
      setReplay(await freezerApi.replaySnapshot(snapshotId));
    } catch (cause) {
      setError(errorMessage(cause));
    } finally {
      setReplaying(false);
    }
  }

  return (
    <div className="content-grid field-page">
      <Link to="/" className="back-link">
        Gare
      </Link>
      <section className="section-heading">
        <div>
          <h1>Il tuo piano</h1>
          <p className="quiet-copy">
            Congelato il {formatDate(snapshot.createdAtUtc)}
          </p>
        </div>
        <span
          className={
            isOffline ? "status status-warning" : "status status-final"
          }
        >
          {isOffline ? "Disponibile offline" : "Piano immutabile"}
        </span>
      </section>
      <div className="notice notice-info">
        <strong>
          Revisione {snapshot.parameterVersion} · modello{" "}
          {snapshot.engineVersion}
        </strong>
        <p>
          Questo piano è una fotografia immutabile. Le note di campo vengono
          aggiunte come eventi separati e ordinati.
        </p>
        {pendingCount > 0 && (
          <p>
            {pendingCount}{" "}
            {pendingCount === 1 ? "nota in attesa" : "note in attesa"} di
            sincronizzazione.
          </p>
        )}
        {!isOffline && (
          <Link
            className="button button-secondary mr-2"
            to={`/field/${encodeURIComponent(snapshot.id)}/verification`}
          >
            Verifica risultati finali
          </Link>
        )}
        {!isOffline && (
          <button
            className="button button-secondary"
            disabled={replaying}
            onClick={() => void replaySnapshot()}
          >
            {replaying ? "Verifica replay…" : "Verifica riproducibilità"}
          </button>
        )}
        {isOffline && (
          <button
            className="button button-secondary"
            onClick={() => void refreshOfflineSnapshot()}
          >
            Verifica copia locale
          </button>
        )}
        {!isOffline && pendingCount > 0 && (
          <button
            className="button button-secondary"
            onClick={() => void synchronize()}
          >
            Sincronizza note
          </button>
        )}
        {!isOffline && (
          <button
            className="button button-secondary"
            disabled={downloading}
            onClick={() => void downloadDocuments()}
          >
            {downloading ? "Preparazione…" : "Scarica documenti gara"}
          </button>
        )}
        {!isOffline && (
          <div className="document-download-actions" aria-label="Scarica immagini PNG">
            <button
              className="button button-secondary"
              disabled={downloadingSide !== null}
              onClick={() => void downloadImage("front")}
            >
              {downloadingSide === "front" ? "Preparazione…" : "Scarica PNG fronte"}
            </button>
            <button
              className="button button-secondary"
              disabled={downloadingSide !== null}
              onClick={() => void downloadImage("back")}
            >
              {downloadingSide === "back" ? "Preparazione…" : "Scarica PNG retro"}
            </button>
          </div>
        )}
      </div>
      {isOffline && (
        <p className="notice notice-warning" role="status">
          Le copie e le note locali non sono cifrate come protezione per un
          dispositivo condiviso. La disconnessione o il logout remoto non
          possono cancellare questo dispositivo; quando la connessione torna,
          accedi e sincronizza, quindi esci per rimuovere i dati locali.
        </p>
      )}
      {error && <ErrorMessage>{error}</ErrorMessage>}
      {status && (
        <p className="notice notice-success" role="status">
          {status}
        </p>
      )}
      <section className="field-stage-list" aria-label="Stime congelate">
        {snapshot.stages.map((stage) => (
          <article className="field-stage" key={stage.stageId}>
            <div className="field-stage-index">
              {String(stage.stageNumber).padStart(2, "0")}
            </div>
            <div>
              <span className="stage-meta">
                Stage · {stage.minimumRounds} colpi minimi
              </span>
              <h2>
                {stage.center === null
                  ? "Stima non disponibile"
                  : stage.center.toFixed(2)}
              </h2>
              <p>
                Intervallo {stage.lower?.toFixed(2) ?? "—"}–
                {stage.upper?.toFixed(2) ?? "—"}
                {stage.rhythmSymbol ? ` · ritmo ${stage.rhythmSymbol}` : ""}
              </p>
            </div>
          </article>
        ))}
      </section>
      {replay && (
        <section className="panel replay-report" aria-live="polite">
          <div className="panel-heading">
            <div>
              <h2>Riproducibilità del piano</h2>
              <p className="quiet-copy">
                Modello {replay.engineVersion} · il replay usa solo gli input
                congelati.
              </p>
            </div>
            <span
              className={
                replay.matchesFrozenForecast
                  ? "status status-final"
                  : "status status-warning"
              }
            >
              {!replay.isReplayAvailable
                ? "Replay non disponibile"
                : replay.matchesFrozenForecast
                  ? "Forecast coincidente"
                  : "Differenza rilevata"}
            </span>
          </div>
          {replay.diagnostics.length > 0 && (
            <ul className="diagnostic-list">
              {replay.diagnostics.map((item, index) => (
                <li key={`${index}-${item}`}>{item}</li>
              ))}
            </ul>
          )}
          {replay.stages.length > 0 && (
            <div className="pdf-table-scroll">
              <table>
                <thead>
                  <tr>
                    <th>Stage</th>
                    <th>Stima congelata</th>
                    <th>Stima replay</th>
                    <th>Intervallo congelato</th>
                    <th>Intervallo replay</th>
                    <th>Confronto</th>
                  </tr>
                </thead>
                <tbody>
                  {replay.stages.map((stage) => {
                    const format = (value: number | null) =>
                      value === null
                        ? "—"
                        : value.toLocaleString("it-IT", {
                            maximumFractionDigits: 4,
                          });
                    return (
                      <tr key={stage.stageId}>
                        <td>{stage.stageNumber}</td>
                        <td>{format(stage.frozenCenter)}</td>
                        <td>{format(stage.replayedCenter)}</td>
                        <td>
                          {format(stage.frozenLower)}–
                          {format(stage.frozenUpper)}
                        </td>
                        <td>
                          {format(stage.replayedLower)}–
                          {format(stage.replayedUpper)}
                        </td>
                        <td>
                          {stage.matchesFrozenForecast
                            ? "Coincide"
                            : "Differente"}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </section>
      )}
      <section className="panel field-notes">
        <div className="panel-heading">
          <div>
            <h2>Aggiungi una nota di gara</h2>
          </div>
        </div>
        <form
          className="form-stack"
          onSubmit={(event) => void submitEvent(event)}
        >
          <label>
            Tipo di nota
            <select
              name="kind"
              value={eventKind}
              onChange={(event) =>
                setEventKind(parseEventKind(event.currentTarget.value))
              }
            >
              <option value="reconnaissance">Ricognizione</option>
              <option value="turn">Turno / ordine</option>
              <option value="midday">Aggiornamento di metà giornata</option>
              <option value="correction">Correzione</option>
            </select>
          </label>
          {eventKind === "turn" && (
            <fieldset className="turn-fields">
              <legend>Contesto di partenza</legend>
              <div className="form-columns">
                <label>
                  Stage di partenza
                  <input name="startStage" type="number" min="1" step="1" />
                </label>
                <label>
                  Numero turno
                  <input name="startTurn" type="number" min="1" step="1" />
                </label>
                <label>
                  Dimensione squadra
                  <input name="squadSize" type="number" min="1" step="1" />
                </label>
              </div>
              <label>
                Ordine stage
                <input
                  name="displayOrder"
                  placeholder="Numeri stage, separati da virgola"
                />
                <span className="field-hint">
                  Indica ogni stage una sola volta, nell’ordine desiderato.
                </span>
              </label>
            </fieldset>
          )}
          <label>
            Dettagli
            <textarea
              name="content"
              rows={4}
              maxLength={2000}
              placeholder="Opzionale: aggiungi un dettaglio o una correzione."
            />
          </label>
          <button className="button button-primary">Salva nota</button>
        </form>
        <ol className="event-list">
          {events.map((item) => (
            <li key={item.id}>
              <span>
                {item.kind} ·{" "}
                {item.sequence > 0 ? `#${item.sequence}` : "in attesa"}
              </span>
              {(item.startStage ||
                item.startTurn ||
                item.squadSize ||
                Boolean(item.displayOrder?.length)) && (
                <small className="event-context">
                  {item.startStage && `Partenza stage ${item.startStage}`}
                  {item.startTurn && ` · turno ${item.startTurn}`}
                  {item.squadSize && ` · squadra ${item.squadSize}`}
                  {item.displayOrder && item.displayOrder.length > 0 &&
                    ` · ordine ${item.displayOrder.map((stageId) => snapshot.stages.find((stage) => stage.stageId === stageId)?.stageNumber ?? stageId).join(", ")}`}
                </small>
              )}
              <p>{item.content}</p>
            </li>
          ))}
        </ol>
      </section>
    </div>
  );
}
