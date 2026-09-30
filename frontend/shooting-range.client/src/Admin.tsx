import { useEffect, useState } from "react";
import { Link } from "react-router";
import { Shell, type SessionViewProps } from "./App";
import {
  ApiError,
  addClosure,
  adminBays,
  adminBookings,
  adminClosures,
  adminConfig,
  adminUsers,
  approveUser,
  decideBooking,
  saveBay,
  saveClosure,
  saveConfig,
  type AdminUser,
  type Bay,
  type Booking,
  type Closure,
  type RangeConfig,
} from "./api";
import { accountStatus, bookingStatus } from "./schedule";

interface AdminData {
  config: RangeConfig;
  users: AdminUser[];
  bays: Bay[];
  closures: Closure[];
  bookings: Booking[];
}
const dayNames = [
  "Domenica",
  "Lunedi",
  "Martedi",
  "Mercoledi",
  "Giovedi",
  "Venerdi",
  "Sabato",
];

function errorMessage(reason: unknown): string {
  if (reason instanceof ApiError && reason.status === 401)
    return "Accedi con un account amministratore.";
  if (reason instanceof ApiError && reason.status === 403)
    return "Account non autorizzato o cambio password richiesto.";
  return reason instanceof Error ? reason.message : "Operazione non riuscita.";
}

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
}: {
  initial: RangeConfig;
  busy: boolean;
  onSave: (config: RangeConfig) => Promise<void>;
}) {
  const [config, setConfig] = useState(initial);
  useEffect(() => setConfig(initial), [initial]);
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
              setConfig({ ...config, name: event.target.value })
            }
          />
        </label>
        <label>
          Fuso orario
          <input
            required
            value={config.timeZone}
            onChange={(event) =>
              setConfig({ ...config, timeZone: event.target.value })
            }
          />
        </label>
        <label className="wide-field">
          Modalita
          <select
            value={config.sessionMode}
            onChange={(event) =>
              setConfig({
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
                setConfig({ ...config, continuousStart: value })
              }
            />
            <TimeField
              label="Chiusura continua"
              value={config.continuousEnd}
              onChange={(value) =>
                setConfig({ ...config, continuousEnd: value })
              }
            />
          </>
        ) : (
          <>
            <TimeField
              label="Apertura mattina"
              value={config.morningStart}
              onChange={(value) =>
                setConfig({ ...config, morningStart: value ?? "" })
              }
            />
            <TimeField
              label="Chiusura mattina"
              value={config.morningEnd}
              onChange={(value) =>
                setConfig({ ...config, morningEnd: value ?? "" })
              }
            />
            <TimeField
              label="Apertura pomeriggio"
              value={config.afternoonStart}
              onChange={(value) =>
                setConfig({ ...config, afternoonStart: value ?? "" })
              }
            />
            <TimeField
              label="Chiusura pomeriggio"
              value={config.afternoonEnd}
              onChange={(value) =>
                setConfig({ ...config, afternoonEnd: value ?? "" })
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
                    setConfig({
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
              setConfig({
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
        <button type="submit">Salva configurazione</button>
      </fieldset>
    </form>
  );
}

export function BayForm({
  initial,
  users,
  busy,
  onSave,
}: {
  initial: Bay;
  users: AdminUser[];
  busy: boolean;
  onSave: (bay: Bay) => Promise<void>;
}) {
  const [bay, setBay] = useState(initial);
  useEffect(() => setBay(initial), [initial]);
  function updateMechanism(
    index: number,
    update: Partial<Bay["mechanisms"][number]>,
  ) {
    setBay({
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
            onChange={(event) => setBay({ ...bay, code: event.target.value })}
          />
        </label>
        <label>
          Stato
          <select
            value={bay.status}
            onChange={(event) =>
              setBay({ ...bay, status: Number(event.target.value) })
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
              setBay({ ...bay, description: event.target.value })
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
              setBay({ ...bay, partitions: Number(event.target.value) })
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
              setBay({ ...bay, plates: Number(event.target.value) })
            }
          />
        </label>
        <label className="check-label">
          <input
            type="checkbox"
            checked={bay.pepper}
            onChange={(event) =>
              setBay({ ...bay, pepper: event.target.checked })
            }
          />
          Pepper
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
                    setBay({
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
          {bay.mechanisms.map((mechanism, index) => (
            <div className="mechanism-row" key={index}>
              <label>
                Tipo
                <input
                  required
                  value={mechanism.type}
                  onChange={(event) =>
                    updateMechanism(index, { type: event.target.value })
                  }
                />
              </label>
              <label>
                Quantita
                <input
                  type="number"
                  required
                  min={1}
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
                className="secondary-button"
                aria-label={`Rimuovi meccanismo ${index + 1}`}
                onClick={() =>
                  setBay({
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
          <button
            type="button"
            className="secondary-button"
            onClick={() =>
              setBay({
                ...bay,
                mechanisms: [...bay.mechanisms, { type: "", quantity: 1 }],
              })
            }
          >
            Aggiungi meccanismo
          </button>
        </div>
        <button type="submit">Salva bay {bay.code}</button>
      </fieldset>
    </form>
  );
}

function ClosureForm({
  bays,
  busy,
  onSave,
}: {
  bays: Bay[];
  busy: boolean;
  onSave: (closure: {
    localDate: string;
    bayId: string | null;
    isClosed: boolean;
    reason: string;
  }) => Promise<void>;
}) {
  const [closure, setClosure] = useState({
    localDate: "",
    bayId: "",
    reason: "",
  });
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
              setClosure({ ...closure, localDate: event.target.value })
            }
          />
        </label>
        <label>
          Campo o bay
          <select
            value={closure.bayId}
            onChange={(event) =>
              setClosure({ ...closure, bayId: event.target.value })
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
              setClosure({ ...closure, reason: event.target.value })
            }
          />
        </label>
        <button type="submit">Aggiungi chiusura</button>
      </fieldset>
    </form>
  );
}

export function Admin({
  account,
  onLogout,
  loggingOut,
}: Partial<SessionViewProps> = {}) {
  const [data, setData] = useState<AdminData | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [newBay, setNewBay] = useState<Bay | null>(null);
  async function load(): Promise<AdminData> {
    const [config, users, bays, closures, bookings] = await Promise.all([
      adminConfig(),
      adminUsers(),
      adminBays(),
      adminClosures(),
      adminBookings(),
    ]);
    return { config, users, bays, closures, bookings };
  }
  useEffect(() => {
    let active = true;
    load()
      .then((result) => {
        if (active) setData(result);
      })
      .catch((reason) => {
        if (active) setError(errorMessage(reason));
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, []);

  async function perform(
    action: () => Promise<unknown>,
    success: string,
  ): Promise<void> {
    setBusy(true);
    setError("");
    setNotice("");
    try {
      await action();
      setData(await load());
      setNotice(success);
      setNewBay(null);
    } catch (reason) {
      setError(errorMessage(reason));
    } finally {
      setBusy(false);
    }
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
      pepper: false,
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
        <h2>Amministrazione</h2>
        <Link to="/">Prenotazioni</Link>
      </section>
      <div aria-live="polite">
        {loading && <p>Caricamento amministrazione...</p>}
        {busy && <p>Salvataggio in corso...</p>}
        {notice && <p className="success">{notice}</p>}
      </div>
      {error && (
        <p role="alert" className="error">
          {error}
        </p>
      )}
      {!loading && !data && <Link to="/">Torna all'accesso</Link>}
      {data && (
        <>
          <section className="admin-section">
            <h3>Configurazione del campo</h3>
            <ConfigForm
              initial={data.config}
              busy={busy}
              onSave={(config) =>
                perform(() => saveConfig(config), "Configurazione salvata.")
              }
            />
          </section>
          <section className="admin-section">
            <div className="section-heading">
              <h3>Bay</h3>
              <button
                disabled={busy || !!newBay}
                className="secondary-button"
                onClick={createBay}
              >
                Nuova bay
              </button>
            </div>
            {!data.bays.length && !newBay && (
              <p className="muted">Nessuna bay configurata.</p>
            )}
            {data.bays.map((bay) => (
              <details key={bay.id}>
                <summary>
                  Bay {bay.code} ·{" "}
                  {["Disponibile", "Manutenzione", "Disabilitata"][
                    bay.status
                  ] ?? "Stato sconosciuto"}
                </summary>
                <BayForm
                  initial={bay}
                  users={data.users}
                  busy={busy}
                  onSave={(value) =>
                    perform(() => saveBay(value), "Bay salvata.")
                  }
                />
              </details>
            ))}
            {newBay && (
              <>
                <BayForm
                  initial={newBay}
                  users={data.users}
                  busy={busy}
                  onSave={(value) =>
                    perform(() => saveBay(value), "Bay creata.")
                  }
                />
                <button
                  className="secondary-button"
                  disabled={busy}
                  onClick={() => setNewBay(null)}
                >
                  Annulla nuova bay
                </button>
              </>
            )}
          </section>
          <section className="admin-section">
            <h3>Chiusure</h3>
            <ClosureForm
              bays={data.bays}
              busy={busy}
              onSave={(closure) =>
                perform(() => addClosure(closure), "Chiusura salvata.")
              }
            />
            {!data.closures.length && (
              <p className="muted">Nessuna eccezione al calendario.</p>
            )}
            <ul className="record-list">
              {data.closures.map((closure) => (
                <li key={closure.id}>
                  <span>
                    {closure.localDate} ·{" "}
                    {closure.bayId
                      ? (data.bays.find((bay) => bay.id === closure.bayId)
                          ?.code ?? closure.bayId)
                      : "Tutto il campo"}{" "}
                    · {closure.reason}
                  </span>
                  <strong>
                    {closure.isClosed ? "Chiuso" : "Nessuna chiusura"}
                  </strong>
                  {closure.isClosed && (
                    <button
                      className="secondary-button"
                      disabled={busy}
                      onClick={() =>
                        void perform(
                          () => saveClosure({ ...closure, isClosed: false }),
                          "Chiusura rimossa.",
                        )
                      }
                    >
                      Riapri
                    </button>
                  )}
                </li>
              ))}
            </ul>
          </section>
          <section className="admin-section">
            <h3>Account</h3>
            <ul className="record-list">
              {data.users.map((user) => (
                <li key={user.id}>
                  <span>
                    <strong>
                      {user.firstName} {user.lastName}
                    </strong>{" "}
                    · {user.username} · {accountStatus(user.status)}
                    {user.forcePasswordChange && " · Cambio password richiesto"}
                  </span>
                  {user.status === 0 && (
                    <button
                      disabled={busy}
                      onClick={() =>
                        void perform(
                          () => approveUser(user.id),
                          "Account approvato.",
                        )
                      }
                    >
                      Approva {user.username}
                    </button>
                  )}
                </li>
              ))}
            </ul>
          </section>
          <section className="admin-section">
            <h3>Richieste di prenotazione</h3>
            {!data.bookings.length && (
              <p className="muted">Nessuna richiesta.</p>
            )}
            <ul className="record-list">
              {data.bookings.map((booking) => (
                <li key={booking.id}>
                  <div>
                    <strong>
                      {booking.localDate} · Bay{" "}
                      {data.bays.find((bay) => bay.id === booking.bayId)
                        ?.code ?? booking.bayId}
                    </strong>
                    <p>
                      {new Date(booking.startUtc).toLocaleString("it-IT", {
                        timeZone: data.config.timeZone,
                      })}{" "}
                      -{" "}
                      {new Date(booking.endUtc).toLocaleTimeString("it-IT", {
                        timeZone: data.config.timeZone,
                        hour: "2-digit",
                        minute: "2-digit",
                      })}{" "}
                      · {bookingStatus(booking.status)}
                    </p>
                    <p>
                      {data.users.find((user) => user.id === booking.userId)
                        ?.username ?? booking.userId}{" "}
                      · {booking.request}
                    </p>
                  </div>
                  <div className="action-group">
                    {booking.status !== 1 && (
                      <button
                        disabled={busy}
                        onClick={() =>
                          void perform(
                            () => decideBooking(booking.id, true),
                            "Prenotazione approvata.",
                          )
                        }
                      >
                        Approva
                      </button>
                    )}
                    {booking.status !== 2 && (
                      <button
                        className="secondary-button"
                        disabled={busy}
                        onClick={() =>
                          void perform(
                            () => decideBooking(booking.id, false),
                            "Prenotazione rifiutata.",
                          )
                        }
                      >
                        Rifiuta
                      </button>
                    )}
                  </div>
                </li>
              ))}
            </ul>
          </section>
        </>
      )}
    </Shell>
  );
}
