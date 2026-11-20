import { useEffect, useState } from "react";
import { Link, useFetcher } from "react-router";
import { TabbedWorkspace, type TabbedWorkspaceTab } from "@morwalpiz/layout";
import { Shell, type SessionViewProps } from "./App";
import type { AdminUser, Bay, Booking, Closure, RangeConfig } from "./api";
import { RangeDeferred, RangeSkeleton, RouteSessionError } from "./Loading";
import type {
  ActionResult,
  AdminRouteData,
  SessionErrorData,
} from "./routeData";
import { accountStatus, bookingStatus } from "./schedule";

type ClosureDraft = {
  localDate: string;
  bayId: string;
  reason: string;
};
const dayNames = [
  "Domenica",
  "Lunedi",
  "Martedi",
  "Mercoledi",
  "Giovedi",
  "Venerdi",
  "Sabato",
];

function TimeField({
  label,
  value,
  onChange,
}: {
  label: string;
  value: string | null;
  onChange: (value: string | null) => void;
}) {
  return (
    <label>
      {label}
      <input
        type="time"
        required
        step={60}
        value={value?.slice(0, 5) ?? ""}
        onChange={(event) =>
          onChange(event.target.value ? `${event.target.value}:00` : null)
        }
      />
    </label>
  );
}

export function ConfigForm({
  initial,
  busy,
  onSave,
  onDraftChange,
}: {
  initial: RangeConfig;
  busy: boolean;
  onSave: (config: RangeConfig) => Promise<void>;
  onDraftChange: (config: RangeConfig) => void;
}) {
  const [config, setConfig] = useState(initial);
  useEffect(() => setConfig(initial), [initial]);
  function updateConfig(value: RangeConfig) {
    setConfig(value);
    onDraftChange(value);
  }
  return (
    <form
      onSubmit={(event) => {
        event.preventDefault();
        void onSave({ ...config, hourlyMinutes: 60 });
      }}
    >
      <fieldset className="form-grid" disabled={busy}>
        <legend>Calendario e sessioni</legend>
        <label>
          Nome del campo
          <input
            required
            maxLength={120}
            value={config.name}
            onChange={(event) =>
              updateConfig({ ...config, name: event.target.value })
            }
          />
        </label>
        <label>
          Fuso orario
          <input
            required
            value={config.timeZone}
            onChange={(event) =>
              updateConfig({ ...config, timeZone: event.target.value })
            }
          />
        </label>
        <label className="wide-field">
          Modalita
          <select
            value={config.sessionMode}
            onChange={(event) =>
              updateConfig({
                ...config,
                sessionMode: Number(
                  event.target.value,
                ) as RangeConfig["sessionMode"],
              })
            }
          >
            <option value={0}>Mattina o pomeriggio intero</option>
            <option value={1}>60 minuti, mattina e pomeriggio con pausa</option>
            <option value={2}>60 minuti, apertura continua</option>
          </select>
        </label>
        {config.sessionMode === 2 ? (
          <>
            <TimeField
              label="Apertura continua"
              value={config.continuousStart}
              onChange={(value) =>
                updateConfig({ ...config, continuousStart: value })
              }
            />
            <TimeField
              label="Chiusura continua"
              value={config.continuousEnd}
              onChange={(value) =>
                updateConfig({ ...config, continuousEnd: value })
              }
            />
          </>
        ) : (
          <>
            <TimeField
              label="Apertura mattina"
              value={config.morningStart}
              onChange={(value) =>
                updateConfig({ ...config, morningStart: value ?? "" })
              }
            />
            <TimeField
              label="Chiusura mattina"
              value={config.morningEnd}
              onChange={(value) =>
                updateConfig({ ...config, morningEnd: value ?? "" })
              }
            />
            <TimeField
              label="Apertura pomeriggio"
              value={config.afternoonStart}
              onChange={(value) =>
                updateConfig({ ...config, afternoonStart: value ?? "" })
              }
            />
            <TimeField
              label="Chiusura pomeriggio"
              value={config.afternoonEnd}
              onChange={(value) =>
                updateConfig({ ...config, afternoonEnd: value ?? "" })
              }
            />
          </>
        )}
        <div className="wide-field">
          <span className="field-label">Giorni di apertura</span>
          <div className="check-group">
            {dayNames.map((name, day) => (
              <label className="check-label" key={day}>
                <input
                  type="checkbox"
                  checked={config.openingDays.includes(day)}
                  onChange={(event) =>
                    updateConfig({
                      ...config,
                      openingDays: event.target.checked
                        ? [...config.openingDays, day]
                        : config.openingDays.filter((value) => value !== day),
                    })
                  }
                />
                {name}
              </label>
            ))}
          </div>
        </div>
        <label>
          Rilascio bay riservate (giorni prima)
          <input
            type="number"
            min={0}
            max={365}
            required
            value={config.reservedReleaseDaysBefore}
            onChange={(event) =>
              updateConfig({
                ...config,
                reservedReleaseDaysBefore: Number(event.target.value),
              })
            }
          />
        </label>
        <div>
          <span className="field-label">Durata slot orari</span>
          <output>60 minuti</output>
        </div>
        {initial.hourlyMinutes !== 60 && (
          <p role="alert" className="error wide-field">
            Durata precedente non valida: {initial.hourlyMinutes} minuti.
            Correggi la configurazione prima di accettare nuove prenotazioni.
          </p>
        )}
        <button type="submit" className="button button-primary">Salva configurazione</button>
      </fieldset>
    </form>
  );
}

export function BayForm({
  initial,
  users,
  mechanismTypes,
  busy,
  onSave,
  onDraftChange,
}: {
  initial: Bay;
  users: AdminUser[];
  mechanismTypes: { id: string; name: string }[];
  busy: boolean;
  onSave: (bay: Bay) => Promise<void>;
  onDraftChange: (bay: Bay) => void;
}) {
  const [bay, setBay] = useState(initial);
  useEffect(() => setBay(initial), [initial]);
  function updateBay(value: Bay) {
    setBay(value);
    onDraftChange(value);
  }
  function updateMechanism(
    index: number,
    update: Partial<Bay["mechanisms"][number]>,
  ) {
    updateBay({
      ...bay,
      mechanisms: bay.mechanisms.map((mechanism, position) =>
        position === index ? { ...mechanism, ...update } : mechanism,
      ),
    });
  }
  return (
    <form
      onSubmit={(event) => {
        event.preventDefault();
        void onSave(bay);
      }}
    >
      <fieldset className="form-grid" disabled={busy}>
        <legend>Bay {initial.code || "nuova"}</legend>
        <label>
          Codice
          <input
            required
            maxLength={40}
            value={bay.code}
            onChange={(event) => updateBay({ ...bay, code: event.target.value })}
          />
        </label>
        <label>
          Stato
          <select
            value={bay.status}
            onChange={(event) =>
              updateBay({ ...bay, status: Number(event.target.value) })
            }
          >
            <option value={0}>Disponibile</option>
            <option value={1}>Manutenzione</option>
            <option value={2}>Disabilitata</option>
          </select>
        </label>
        <label className="wide-field">
          Descrizione
          <input
            maxLength={1000}
            value={bay.description}
            onChange={(event) =>
              updateBay({ ...bay, description: event.target.value })
            }
          />
        </label>
        <label>
          Paratie
          <input
            type="number"
            min={0}
            required
            value={bay.partitions}
            onChange={(event) =>
              updateBay({ ...bay, partitions: Number(event.target.value) })
            }
          />
        </label>
        <label>
          Piastre
          <input
            type="number"
            min={0}
            required
            value={bay.plates}
            onChange={(event) =>
              updateBay({ ...bay, plates: Number(event.target.value) })
            }
          />
        </label>
        <label>
          Pepper
          <input
            type="number"
            min={0}
            step={1}
            required
            value={bay.pepper}
            onChange={(event) =>
              updateBay({ ...bay, pepper: Number(event.target.value) })
            }
          />
        </label>
        <div className="wide-field">
          <span className="field-label">Utenti in whitelist</span>
          <div className="check-group">
            {users.map((user) => (
              <label className="check-label" key={user.id}>
                <input
                  type="checkbox"
                  checked={bay.whitelistUserIds.includes(user.id)}
                  onChange={(event) =>
                    updateBay({
                      ...bay,
                      whitelistUserIds: event.target.checked
                        ? [...bay.whitelistUserIds, user.id]
                        : bay.whitelistUserIds.filter((id) => id !== user.id),
                    })
                  }
                />
                {user.username}
              </label>
            ))}
          </div>
          {!users.length && <p className="muted">Nessun utente.</p>}
        </div>
        <div className="wide-field">
          <span className="field-label">Meccanismi</span>
          {bay.mechanisms.length > 0 && (
            <div className="mechanism-list">
              {bay.mechanisms.map((mechanism, index) => (
                <div className="mechanism-row" key={index}>
                  <label>
                    Tipo
                    <select
                      required
                      value={mechanism.type}
                      onChange={(event) =>
                        updateMechanism(index, { type: event.target.value })
                      }
                    >
                      <option value="">Seleziona un tipo</option>
                      {mechanismTypes.map((type) => (
                        <option key={type.id} value={type.name}>
                          {type.name}
                        </option>
                      ))}
                    </select>
                  </label>
                  <label>
                    Quantita
                    <input
                      type="number"
                      required
                      min={1}
                      step={1}
                      value={mechanism.quantity}
                      onChange={(event) =>
                        updateMechanism(index, {
                          quantity: Number(event.target.value),
                        })
                      }
                    />
                  </label>
                  <button
                    type="button"
                    className="button button-quiet"
                    aria-label={`Rimuovi meccanismo ${index + 1}`}
                    onClick={() =>
                      updateBay({
                        ...bay,
                        mechanisms: bay.mechanisms.filter(
                          (_, position) => position !== index,
                        ),
                      })
                    }
                  >
                    Rimuovi
                  </button>
                </div>
              ))}
            </div>
          )}
          {!mechanismTypes.length && (
            <p className="range-muted">Crea prima un tipo di meccanismo.</p>
          )}
          <button
            type="button"
            className="button button-quiet"
            disabled={!mechanismTypes.length}
            onClick={() =>
              updateBay({
                ...bay,
                mechanisms: [
                  ...bay.mechanisms,
                  { type: mechanismTypes[0].name, quantity: 1 },
                ],
              })
            }
          >
            Aggiungi meccanismo
          </button>
        </div>
        <button type="submit" className="button button-primary">
          Salva bay {bay.code}
        </button>
      </fieldset>
    </form>
  );
}

function ClosureForm({
  bays,
  busy,
  onSave,
  closure,
  onDraftChange,
}: {
  bays: Bay[];
  busy: boolean;
  closure: ClosureDraft;
  onDraftChange: (closure: ClosureDraft) => void;
  onSave: (closure: {
    localDate: string;
    bayId: string | null;
    isClosed: boolean;
    reason: string;
  }) => Promise<void>;
}) {
  return (
    <form
      onSubmit={(event) => {
        event.preventDefault();
        void onSave({
          ...closure,
          bayId: closure.bayId || null,
          isClosed: true,
        });
      }}
    >
      <fieldset className="form-grid" disabled={busy}>
        <legend>Nuova chiusura</legend>
        <label>
          Data
          <input
            type="date"
            required
            value={closure.localDate}
            onChange={(event) =>
              onDraftChange({ ...closure, localDate: event.target.value })
            }
          />
        </label>
        <label>
          Campo o bay
          <select
            value={closure.bayId}
            onChange={(event) =>
              onDraftChange({ ...closure, bayId: event.target.value })
            }
          >
            <option value="">Tutto il campo</option>
            {bays.map((bay) => (
              <option key={bay.id} value={bay.id}>
                {bay.code}
              </option>
            ))}
          </select>
        </label>
        <label className="wide-field">
          Motivo
          <input
            required
            maxLength={1000}
            value={closure.reason}
            onChange={(event) =>
              onDraftChange({ ...closure, reason: event.target.value })
            }
          />
        </label>
        <button type="submit" className="button button-primary">Aggiungi chiusura</button>
      </fieldset>
    </form>
  );
}

export function Admin({
  account,
  onLogout,
  loggingOut,
}: Partial<SessionViewProps> = {}) {
  const [selectedTab, setSelectedTab] = useState("config");
  const [configDraft, setConfigDraft] = useState<RangeConfig | null>(null);
  const [bayDrafts, setBayDrafts] = useState<Record<string, Bay>>({});
  const [closureDraft, setClosureDraft] = useState<ClosureDraft>({
    localDate: "",
    bayId: "",
    reason: "",
  });
  const [newBay, setNewBay] = useState<Bay | null>(null);
  const [mechanismTypeName, setMechanismTypeName] = useState("");
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const routeFetcher = useFetcher<AdminRouteData | SessionErrorData | null>();
  const actionFetcher = useFetcher<ActionResult>();
  const busy = actionFetcher.state !== "idle";

  useEffect(() => {
    routeFetcher.load("/admin");
  }, [account?.id, routeFetcher.load]);

  useEffect(() => {
    const result = actionFetcher.data;
    if (!result) return;
    setError("");
    if (result.success) {
      setNotice(result.message);
      if (result.intent === "save-config") setConfigDraft(null);
      if (result.intent === "create-mechanism-type") {
        setMechanismTypeName("");
        routeFetcher.load("/admin");
      }
      if (result.intent === "save-bay" && result.entityId) {
        setBayDrafts((value) => {
          const next = { ...value };
          delete next[result.entityId!];
          return next;
        });
        setNewBay((value) => value?.id === result.entityId ? null : value);
      }
      if (result.intent === "add-closure") {
        setClosureDraft({ localDate: "", bayId: "", reason: "" });
      }
    } else {
      setNotice("");
      setError(result.message);
    }
  }, [actionFetcher.data]);

  const routeError =
    routeFetcher.data && "sessionError" in routeFetcher.data
      ? routeFetcher.data.sessionError
      : null;
  const routeData =
    routeFetcher.data && "core" in routeFetcher.data
      ? routeFetcher.data
      : null;

  function perform(intent: string, payload?: object) {
    if (busy) return;
    const formData = new FormData();
    formData.set("intent", intent);
    if (payload) formData.set("payload", JSON.stringify(payload));
    setError("");
    setNotice("");
    actionFetcher.submit(formData, { method: "post", action: "/admin" });
  }

  function createBay() {
    setNewBay({
      id: crypto.randomUUID(),
      creationDateTime: new Date().toISOString(),
      code: "",
      description: "",
      mechanisms: [],
      partitions: 0,
      plates: 0,
      pepper: 0,
      status: 0,
      reservedUntilUtc: null,
      whitelistUserIds: [],
    });
  }

  return (
    <Shell
      account={account ?? null}
      onLogout={onLogout}
      loggingOut={loggingOut}
    >
      <section className="booking-heading">
        <div>
          <h1>Amministrazione</h1>
          <p className="range-muted">Gestisci calendario, postazioni e richieste.</p>
        </div>
        <Link className="button button-quiet" to="/">Prenotazioni</Link>
      </section>
      {busy && <p className="range-pending" role="status">Salvataggio in corso...</p>}
      {notice && <p className="range-success" role="status">{notice}</p>}
      {error && (
        <p role="alert" className="range-error">{error}</p>
      )}
      {routeError && (
        <RouteSessionError
          message={routeError.message ?? "Sessione non disponibile."}
          status={routeError.status}
          onRetry={() => routeFetcher.load("/admin")}
        />
      )}
      {!routeError && !routeData && (
        <RangeSkeleton rows={5} label="Caricamento amministrazione..." />
      )}
      {!routeError && routeData && (
        <RangeDeferred
          resolve={routeData.core}
          fallback={<RangeSkeleton rows={5} label="Caricamento amministrazione..." />}
          onRetry={() => routeFetcher.load("/admin")}
        >
          {(core) => {
            const tabs: TabbedWorkspaceTab[] = [
              {
                key: "config",
                label: "Calendario",
                panel: (
                  <section className="range-panel">
                    <h2>Configurazione del campo</h2>
                    <ConfigForm
                      initial={configDraft ?? core.config}
                      busy={busy}
                      onDraftChange={setConfigDraft}
                      onSave={async (config) => perform("save-config", config)}
                    />
                  </section>
                ),
                disabled: busy,
              },
              {
                key: "mechanism-types",
                label: "Meccanismi",
                panel: (
                  <section className="range-panel">
                    <h2>Tipi di meccanismo</h2>
                    <p className="range-muted">
                      Crea i tipi disponibili per la configurazione delle postazioni.
                    </p>
                    <form
                      className="mechanism-type-form"
                      onSubmit={(event) => {
                        event.preventDefault();
                        perform("create-mechanism-type", { name: mechanismTypeName });
                      }}
                    >
                      <label>
                        Nome del tipo
                        <input
                          required
                          maxLength={80}
                          value={mechanismTypeName}
                          onChange={(event) => setMechanismTypeName(event.target.value)}
                        />
                      </label>
                      <button
                        type="submit"
                        className="button button-primary"
                        disabled={busy || !mechanismTypeName.trim()}
                      >
                        Crea tipo
                      </button>
                    </form>
                    {core.mechanismTypes.length ? (
                      <ul className="record-list">
                        {core.mechanismTypes.map((type) => (
                          <li key={type.id}><strong>{type.name}</strong></li>
                        ))}
                      </ul>
                    ) : (
                      <p className="range-empty">
                        Nessun tipo creato. Aggiungi un tipo per configurare i meccanismi delle postazioni.
                      </p>
                    )}
                  </section>
                ),
                disabled: busy,
              },
              {
                key: "bays",
                label: "Postazioni",
                panel: (
                  <section className="range-panel">
                    <div className="section-heading mb-2">
                      <h2>Postazioni</h2>
                      <button
                        type="button"
                        disabled={busy || !!newBay}
                        className="button button-quiet"
                        onClick={createBay}
                      >
                        Nuova postazione
                      </button>
                    </div>
                    {!core.bays.length && !newBay && (
                      <p className="range-empty">Nessuna postazione configurata.</p>
                    )}
                    {core.bays.map((bay) => (
                      <details key={bay.id}>
                        <summary>
                          Postazione {bay.code} ·{" "}
                          {["Disponibile", "Manutenzione", "Disabilitata"][bay.status] ?? "Stato sconosciuto"}
                        </summary>
                        <BayForm
                          initial={bayDrafts[bay.id] ?? bay}
                          users={core.users}
                          mechanismTypes={core.mechanismTypes}
                          busy={busy}
                          onDraftChange={(value) => setBayDrafts((drafts) => ({ ...drafts, [bay.id]: value }))}
                          onSave={async (value) => perform("save-bay", value)}
                        />
                      </details>
                    ))}
                    {newBay && (
                      <>
                        <BayForm
                          initial={newBay}
                          users={core.users}
                          mechanismTypes={core.mechanismTypes}
                          busy={busy}
                          onDraftChange={setNewBay}
                          onSave={async (value) => perform("save-bay", value)}
                        />
                        <button
                          type="button"
                          className="button button-quiet"
                          disabled={busy}
                          onClick={() => setNewBay(null)}
                        >
                          Annulla nuova postazione
                        </button>
                      </>
                    )}
                  </section>
                ),
                disabled: busy,
              },
              {
                key: "closures",
                label: "Chiusure",
                panel: (
                  <section className="range-panel">
                    <h2>Chiusure</h2>
                    <ClosureForm
                      bays={core.bays}
                      busy={busy}
                      closure={closureDraft}
                      onDraftChange={setClosureDraft}
                      onSave={async (value) => perform("add-closure", value)}
                    />
                    <RangeDeferred
                      resolve={routeData.closures}
                      fallback={<RangeSkeleton rows={3} label="Caricamento chiusure..." />}
                      onRetry={() => routeFetcher.load("/admin")}
                    >
                      {(closures) => closures.length ? (
                        <ul className="record-list">
                          {closures.map((closure) => (
                            <li key={closure.id}>
                              <span>
                                {closure.localDate} ·{" "}
                                {closure.bayId
                                  ? (core.bays.find((bay) => bay.id === closure.bayId)?.code ?? closure.bayId)
                                  : "Tutto il campo"}{" "}
                                · {closure.reason}
                              </span>
                              <strong>{closure.isClosed ? "Chiuso" : "Nessuna chiusura"}</strong>
                              {closure.isClosed && (
                                <button
                                  type="button"
                                  className="button button-quiet"
                                  disabled={busy}
                                  onClick={() => perform("save-closure", { ...closure, isClosed: false })}
                                >
                                  Riapri
                                </button>
                              )}
                            </li>
                          ))}
                        </ul>
                      ) : <p className="range-empty">Nessuna eccezione al calendario.</p>}
                    </RangeDeferred>
                  </section>
                ),
                disabled: busy,
              },
              {
                key: "accounts",
                label: "Account",
                panel: (
                  <section className="range-panel">
                    <h2>Account</h2>
                    <ul className="record-list">
                      {core.users.map((user) => (
                        <li key={user.id}>
                          <span>
                            <strong>{user.firstName} {user.lastName}</strong> · {user.username} · {accountStatus(user.status)}
                            {user.forcePasswordChange && " · Cambio password richiesto"}
                          </span>
                          {user.status === 0 && (
                            <button
                              type="button"
                              className="button button-primary"
                              disabled={busy}
                              onClick={() => perform("approve-user", { id: user.id })}
                            >
                              Approva {user.username}
                            </button>
                          )}
                        </li>
                      ))}
                    </ul>
                  </section>
                ),
                disabled: busy,
              },
              {
                key: "bookings",
                label: "Prenotazioni",
                panel: (
                  <section className="range-panel">
                    <h2>Richieste di prenotazione</h2>
                    <RangeDeferred
                      resolve={routeData.bookings}
                      fallback={<RangeSkeleton rows={4} label="Caricamento richieste..." />}
                      onRetry={() => routeFetcher.load("/admin")}
                    >
                      {(data) => data.bookings.length ? (
                        <ul className="record-list">
                          {data.bookings.map((booking) => (
                            <li key={booking.id}>
                              <div>
                                <strong>
                                  {booking.localDate} · Postazione{" "}
                                  {data.bays.find((bay) => bay.id === booking.bayId)?.code ?? booking.bayId}
                                </strong>
                                <p>
                                  {new Date(booking.startUtc).toLocaleString("it-IT", { timeZone: data.config.timeZone })}{" "}
                                  -{" "}
                                  {new Date(booking.endUtc).toLocaleTimeString("it-IT", {
                                    timeZone: data.config.timeZone,
                                    hour: "2-digit",
                                    minute: "2-digit",
                                  })}{" "}
                                  · {bookingStatus(booking.status)}
                                </p>
                                <p>
                                  {data.users.find((user) => user.id === booking.userId)?.username ?? booking.userId}
                                  {" · "}{booking.request}
                                </p>
                              </div>
                              <div className="action-group">
                                {booking.status !== 1 && (
                                  <button
                                    type="button"
                                    className="button button-primary"
                                    disabled={busy}
                                    onClick={() => perform("decide-booking", { id: booking.id, approved: true })}
                                  >
                                    Approva
                                  </button>
                                )}
                                {booking.status !== 2 && (
                                  <button
                                    type="button"
                                    className="button button-quiet"
                                    disabled={busy}
                                    onClick={() => perform("decide-booking", { id: booking.id, approved: false })}
                                  >
                                    Rifiuta
                                  </button>
                                )}
                              </div>
                            </li>
                          ))}
                        </ul>
                      ) : <p className="range-empty">Nessuna richiesta.</p>}
                    </RangeDeferred>
                  </section>
                ),
                disabled: busy,
              },
            ];
            return (
              <TabbedWorkspace
                aria-label="Sezioni di amministrazione"
                tabs={tabs}
                selectedKey={selectedTab}
                onSelectedKeyChange={setSelectedTab}
                className="range-admin-tabs"
                tabListClassName="range-admin-tabs__list"
                tabClassName="range-admin-tabs__tab"
                panelClassName="range-admin-tabs__panel"
              />
            );
          }}
        </RangeDeferred>
      )}
    </Shell>
  );
}
