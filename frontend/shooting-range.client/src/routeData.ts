import type { ActionFunctionArgs, LoaderFunctionArgs } from "react-router";
import {
  ApiError,
  acceptFriendRequest,
  addClosure,
  cancelFriendRequest,
  declineFriendRequest,
  friendRequests,
  adminBays,
  adminBookings,
  adminBookingReport,
  adminBookingHistory,
  adminBookingPolicy,
  adminCancelBooking,
  setBookingAttendance,
  adminClosures,
  adminConfig,
  adminMechanismTypes,
  adminUsers,
  approveUser,
  availability,
  book,
  cancelBooking,
  changePassword,
  decideBooking,
  decideMessageStatus,
  createMechanismType,
  createAdminBooking,
  getConfig,
  getSession,
  friends,
  login,
  logout,
  messages,
  myBookings,
  openMessage,
  sendFriendRequest,
  saveBay,
  saveClosure,
  saveConfig,
  saveBookingPolicy,
  removeFriend,
  sessions,
  type Account,
  type AdminUser,
  type Availability,
  type Bay,
  type Closure,
  type RangeConfig,
  type BookingPolicy,
  type BookingPage,
  type MechanismType,
  type MessageStatusDecision,
  type MessageThread,
  type Booking,
  type Friend,
  type FriendRequestLists,
  type Sessions,
} from "./api";

export interface ActionResult {
  intent: string;
  success: boolean;
  message: string;
  status?: number;
  account?: Account;
  entityId?: string;
}

export interface SessionResult {
  error: true;
  message?: string;
  status?: number;
}

export interface BookingRouteData {
  date: string;
  period: string;
  offered: Sessions;
  config: RangeConfig;
  history: Promise<Booking[]>;
  availability: Promise<Availability | null>;
}

export interface SessionErrorData {
  sessionError: SessionResult;
}

export interface AdminCoreData {
  config: RangeConfig;
  bookingPolicy: BookingPolicy;
  users: Awaited<ReturnType<typeof adminUsers>>;
  bays: Awaited<ReturnType<typeof adminBays>>;
  mechanismTypes: MechanismType[];
}

export interface MessagesRouteData {
  items: Promise<{
    threads: MessageThread[];
    users: AdminUser[];
  }>;
}

export interface AdminRouteData {
  core: Promise<AdminCoreData>;
  closures: Promise<Closure[]>;
}

export interface AdminBookingsRouteData {
  config: RangeConfig;
  bookingPolicy: BookingPolicy;
  users: AdminUser[];
  bays: Bay[];
  bookings: Booking[];
  date: string;
  offered: Sessions;
  period: string;
  availability: Availability | null;
  historyDate: string;
  history: Awaited<ReturnType<typeof adminBookingHistory>>;
}

export interface AdminReportRouteData {
  users: AdminUser[];
  config: RangeConfig;
  userId: string;
  attendance: "all" | "show" | "no-show";
  report: BookingPage | null;
  reportError?: SessionResult;
}

export interface FriendsRouteData {
  friends: Friend[];
  requests: FriendRequestLists;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function actionIntent(formData: FormData): string {
  const intent = formData.get("intent");
  return typeof intent === "string" ? intent : "";
}

function actionError(intent: string, reason: unknown): ActionResult {
  if (reason instanceof ApiError && reason.status === 401) {
    if (intent === "login") {
      return {
        intent,
        success: false,
        status: 401,
        message: "Username o password non validi, oppure account non approvato.",
      };
    }
    if (typeof window !== "undefined" && intent !== "login") {
      window.dispatchEvent(new Event("range-session-expired"));
    }
    return { intent, success: false, status: 401, message: "Sessione scaduta. Accedi di nuovo." };
  }
  if (reason instanceof ApiError && reason.status === 403) {
    return { intent, success: false, status: 403, message: "Account non autorizzato o cambio password richiesto." };
  }
  if (intent === "login" && reason instanceof ApiError && reason.status === 429) {
    return { intent, success: false, status: 429, message: "Troppi tentativi. Riprova tra cinque minuti." };
  }
  return {
    intent,
    success: false,
    status: reason instanceof ApiError ? reason.status : undefined,
    message: reason instanceof Error ? reason.message : "Operazione non riuscita.",
  };
}

function jsonField(formData: FormData, name: string): Record<string, unknown> | null {
  const value = formData.get(name);
  if (typeof value !== "string") return null;
  try {
    const parsed: unknown = JSON.parse(value);
    return isRecord(parsed) ? parsed : null;
  } catch {
    return null;
  }
}

function readSessionResult(reason: unknown): SessionResult {
  if (reason instanceof ApiError) {
    return {
      error: true,
      status: reason.status,
      message: reason.status === 401
        ? "Sessione scaduta. Accedi di nuovo."
        : reason.message,
    };
  }
  return {
    error: true,
    message: reason instanceof Error ? reason.message : "Sessione non disponibile.",
  };
}

async function browserAccount(request: Request): Promise<Account | SessionResult> {
  try {
    return await getSession(request.signal);
  } catch (reason) {
    return readSessionResult(reason);
  }
}

function isSessionResult(value: Account | SessionResult): value is SessionResult {
  return "error" in value;
}

export async function bookingLoader({ request }: LoaderFunctionArgs) {
  if (typeof window === "undefined") return null;
  const account = await browserAccount(request);
  if (isSessionResult(account)) return { sessionError: account };
  if (account.forcePasswordChange) return { forcePasswordChange: true };

  const url = new URL(request.url);
  const date = url.searchParams.get("date") ?? new Date().toISOString().slice(0, 10);
  const period = url.searchParams.get("period") ?? "";
  const search = url.searchParams.get("search") === "1";
  let offered: Sessions;
  let config: RangeConfig;
  try {
    [offered, config] = await Promise.all([
      sessions(date, request.signal),
      getConfig(request.signal),
    ]);
  } catch (reason) {
    return { sessionError: readSessionResult(reason) };
  }
  const history = url.searchParams.get("region") === "availability"
    ? Promise.resolve([])
    : myBookings(request.signal);
  const available: Promise<Availability | null> = search && period
    ? Promise.resolve().then(async () => {
        if (!offered.sessions.some((item) => item.periodKey === period)) {
          throw new Error("Gli orari sono cambiati. Seleziona una sessione aggiornata.");
        }
        return availability(date, period, request.signal);
      })
    : Promise.resolve(null);

  return { date, period, offered, config, history, availability: available };
}

export async function bookingAction({ request }: ActionFunctionArgs): Promise<ActionResult> {
  const formData = await request.formData();
  const intent = actionIntent(formData);
  try {
    if (intent === "login") {
      const username = formData.get("username");
      const password = formData.get("password");
      if (
        typeof username !== "string" ||
        !username.trim() ||
        typeof password !== "string" ||
        !password
      ) {
        return { intent, success: false, message: "Inserisci username e password." };
      }
      const account = await login({ username, password });
      return { intent, success: true, message: "", account };
    }

    if (intent === "logout" || intent === "password-change" || intent === "book" || intent === "cancel-booking") {
      const account = await getSession(request.signal);
      if (intent === "logout") {
        await logout();
        return { intent, success: true, message: "" };
      }
      if (account.forcePasswordChange && intent !== "password-change") {
        return { intent, success: false, status: 403, message: "Aggiorna la password prima di continuare." };
      }
      if (intent === "cancel-booking") {
        const id = formData.get("id");
        if (typeof id !== "string" || !id) {
          return { intent, success: false, message: "Prenotazione non valida." };
        }
        await cancelBooking(id);
        return { intent, success: true, message: "Prenotazione annullata. La registrazione resta visibile nello storico." };
      }
      if (intent === "password-change") {
        const currentPassword = formData.get("currentPassword");
        const newPassword = formData.get("newPassword");
        if (typeof currentPassword !== "string" || typeof newPassword !== "string" || newPassword.length < 7) {
          return { intent, success: false, message: "Inserisci la password attuale e una nuova password di almeno 7 caratteri." };
        }
        await changePassword({ currentPassword, newPassword });
        return { intent, success: true, message: "Password aggiornata." };
      }

      const bayId = formData.get("bayId");
      const localDate = formData.get("localDate");
      const periodKey = formData.get("periodKey");
      const expectedStartUtc = formData.get("expectedStartUtc");
      const expectedEndUtc = formData.get("expectedEndUtc");
      const requestText = formData.get("request");
      if (typeof requestText !== "string" || requestText.length > 1000) {
        return { intent, success: false, message: "La nota non può superare 1000 caratteri." };
      }
      if (
        typeof bayId !== "string" ||
        !bayId ||
        typeof localDate !== "string" ||
        !localDate ||
        typeof periodKey !== "string" ||
        !periodKey ||
        typeof expectedStartUtc !== "string" ||
        !expectedStartUtc ||
        typeof expectedEndUtc !== "string" ||
        !expectedEndUtc
      ) {
        return { intent, success: false, message: "Seleziona di nuovo data, sessione e postazione." };
      }
      const offered = await sessions(localDate, request.signal);
      const selected = offered.sessions.find((item) => item.periodKey === periodKey);
      if (!selected || selected.startUtc !== expectedStartUtc || selected.endUtc !== expectedEndUtc) {
        return { intent, success: false, message: "Gli orari sono cambiati. Seleziona una sessione aggiornata." };
      }
      await book({
        bayId,
        localDate,
        periodKey,
        request: requestText,
        expectedStartUtc,
        expectedEndUtc,
      });
      return { intent, success: true, message: "Richiesta inviata, in attesa di approvazione." };
    }

    return { intent, success: false, message: "Operazione non riconosciuta." };
  } catch (reason) {
    if (intent === "password-change" && reason instanceof ApiError && reason.status === 401) {
      try {
        await getSession(request.signal);
        return {
          intent,
          success: false,
          status: 401,
          message: "La password attuale non è corretta.",
        };
      } catch (sessionReason) {
        if (sessionReason instanceof ApiError && sessionReason.status === 401) {
          if (typeof window !== "undefined") {
            window.dispatchEvent(new Event("range-session-expired"));
          }
          return {
            intent,
            success: false,
            status: 401,
            message: "Sessione scaduta. Accedi di nuovo.",
          };
        }
        return {
          intent,
          success: false,
          status: sessionReason instanceof ApiError ? sessionReason.status : undefined,
          message: "Impossibile verificare la sessione. Riprova.",
        };
      }
    }
    if (intent === "book" && reason instanceof ApiError && reason.status === 409) {
      return {
        intent,
        success: false,
        status: 409,
        message: "La postazione non è più disponibile. Seleziona un altro turno.",
      };
    }
    if (intent === "cancel-booking" && reason instanceof ApiError && reason.status === 409) {
      return {
        intent,
        success: false,
        status: 409,
        message: reason.message || "Il termine per annullare è scaduto. Contatta l'amministrazione del campo.",
      };
    }
    return actionError(intent, reason);
  }
}

export async function messagesLoader({ request }: LoaderFunctionArgs) {
  if (typeof window === "undefined") return null;
  const account = await browserAccount(request);
  if (isSessionResult(account)) return { sessionError: account };
  if (account.forcePasswordChange) return { sessionError: { error: true, status: 403, message: "Aggiorna la password prima di continuare." } };
  const users = account.isAdmin ? adminUsers(request.signal) : Promise.resolve([]);
  return {
    items: Promise.all([messages(request.signal), users])
      .then(([threads, adminUsersList]) => ({ threads, users: adminUsersList })),
  };
}

export async function messagesAction({ request }: ActionFunctionArgs): Promise<ActionResult> {
  const formData = await request.formData();
  const intent = actionIntent(formData);
  try {
    const account = await getSession(request.signal);
    if (account.forcePasswordChange) {
      return { intent, success: false, status: 403, message: "Aggiorna la password prima di continuare." };
    }
    if (intent === "decide-message-status") {
      if (!account.isAdmin) {
        return { intent, success: false, status: 403, message: "Account non autorizzato." };
      }
      const payload = jsonField(formData, "payload");
      const id = payload?.id;
      const status = payload?.status;
      if (typeof id !== "string" || !id || !isMessageStatusDecision(status)) {
        return { intent, success: false, message: "Decisione sul messaggio non valida." };
      }
      await decideMessageStatus(id, status);
      return {
        intent,
        success: true,
        message: `Stato del messaggio aggiornato: ${messageStatusLabel(status)}.`,
      };
    }
    if (intent === "send-message") {
      const text = formData.get("text");
      if (typeof text !== "string" || text.trim().length < 3 || text.length > 2000) {
        return { intent, success: false, message: "Il messaggio deve contenere da 3 a 2000 caratteri." };
      }
      await openMessage({ subject: "Richiesta dal portale", text });
      return { intent, success: true, message: "Messaggio inviato." };
    }
    return { intent, success: false, message: "Operazione non riconosciuta." };
  } catch (reason) {
    return actionError(intent, reason);
  }
}

function isMessageStatusDecision(value: unknown): value is MessageStatusDecision {
  return value === 1 || value === 2 || value === 3;
}

function messageStatusLabel(status: MessageStatusDecision): string {
  return status === 1 ? "Presa in carico" : status === 2 ? "Completata" : "Rifiutata";
}

export async function adminLoader({ request }: LoaderFunctionArgs) {
  if (typeof window === "undefined") return null;
  const account = await browserAccount(request);
  if (isSessionResult(account)) return { sessionError: account };
  if (!account.isAdmin) return { sessionError: { error: true, status: 403, message: "Account non autorizzato." } };
  if (account.forcePasswordChange) return { sessionError: { error: true, status: 403, message: "Aggiorna la password prima di continuare." } };

  const core = Promise.all([
    adminConfig(request.signal),
    adminBookingPolicy(request.signal),
    adminUsers(request.signal),
    adminBays(request.signal),
    adminMechanismTypes(request.signal),
  ]).then(([config, bookingPolicy, users, bays, mechanismTypes]) => ({ config, bookingPolicy, users, bays, mechanismTypes }));
  const closures = adminClosures(request.signal);
  return { core, closures };
}

export async function adminBookingsLoader({ request }: LoaderFunctionArgs) {
  if (typeof window === "undefined") return null;
  const account = await browserAccount(request);
  if (isSessionResult(account)) return { sessionError: account };
  if (!account.isAdmin) return { sessionError: { error: true, status: 403, message: "Account non autorizzato." } };
  if (account.forcePasswordChange) return { sessionError: { error: true, status: 403, message: "Aggiorna la password prima di continuare." } };

  try {
    const url = new URL(request.url);
    const date = url.searchParams.get("date") ?? new Date().toISOString().slice(0, 10);
    const period = url.searchParams.get("period") ?? "";
    const config = await adminConfig(request.signal);
    const historyDate = url.searchParams.get("historyDate") ?? fieldLocalDate(new Date(), config.timeZone);
    const page = Math.max(0, Number(url.searchParams.get("historyPage") ?? "0") || 0);
    const [bookingPolicy, users, bays, bookings, offered, history] = await Promise.all([
      adminBookingPolicy(request.signal),
      adminUsers(request.signal),
      adminBays(request.signal),
      adminBookings(request.signal),
      sessions(date, request.signal),
      adminBookingHistory(historyDate, page, 25, request.signal),
    ]);
    const selected = offered.sessions.find((item) => item.periodKey === period);
    const available = selected ? await availability(date, period, request.signal) : null;
    return { config, bookingPolicy, users, bays, bookings, date, offered, period, availability: available, historyDate, history };
  } catch (reason) {
    return { sessionError: readSessionResult(reason) };
  }
}

function fieldLocalDate(now: Date, timeZone: string): string {
  const parts = new Intl.DateTimeFormat("en-US", {
    timeZone,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).formatToParts(now);
  const values = Object.fromEntries(parts.map(({ type, value }) => [type, value]));
  return `${values.year}-${values.month}-${values.day}`;
}

export async function adminReportLoader({ request }: LoaderFunctionArgs) {
  if (typeof window === "undefined") return null;
  const account = await browserAccount(request);
  if (isSessionResult(account)) return { sessionError: account };
  if (!account.isAdmin) return { sessionError: { error: true, status: 403, message: "Account non autorizzato." } };
  if (account.forcePasswordChange) return { sessionError: { error: true, status: 403, message: "Aggiorna la password prima di continuare." } };

  const url = new URL(request.url);
  const userId = url.searchParams.get("userId") ?? "";
  const filter = url.searchParams.get("attendance") ?? "all";
  if (filter !== "all" && filter !== "show" && filter !== "no-show")
    return { sessionError: { error: true, status: 400, message: "Filtro di presenza non valido." } };
  const attendance = filter;

  try {
    const [users, config] = await Promise.all([
      adminUsers(request.signal),
      adminConfig(request.signal),
    ]);
    if (!userId) return { users, config, userId, attendance, report: null };
    const page = Math.max(0, Number(url.searchParams.get("page") ?? "0") || 0);
    try {
      const report = await adminBookingReport(userId, attendance, page, 25, request.signal);
      return { users, config, userId, attendance, report };
    } catch (reason) {
      return { users, config, userId, attendance, report: null, reportError: readSessionResult(reason) };
    }
  } catch (reason) {
    return { sessionError: readSessionResult(reason) };
  }
}

export async function friendsLoader({ request }: LoaderFunctionArgs) {
  if (typeof window === "undefined") return null;
  const account = await browserAccount(request);
  if (isSessionResult(account)) return { sessionError: account };
  if (account.forcePasswordChange) {
    return { sessionError: { error: true, status: 403, message: "Aggiorna la password prima di continuare." } };
  }
  try {
    const [acceptedFriends, requests] = await Promise.all([
      friends(request.signal),
      friendRequests(request.signal),
    ]);
    return { friends: acceptedFriends, requests };
  } catch (reason) {
    return { sessionError: readSessionResult(reason) };
  }
}

export async function friendsAction({ request }: ActionFunctionArgs): Promise<ActionResult> {
  const formData = await request.formData();
  const intent = actionIntent(formData);
  try {
    const account = await getSession(request.signal);
    if (account.forcePasswordChange) {
      return { intent, success: false, status: 403, message: "Aggiorna la password prima di continuare." };
    }
    if (intent === "send-friend-request") {
      const username = formData.get("username");
      if (typeof username !== "string" || !username.trim() || username.length > 80) {
        return { intent, success: false, message: "Inserisci uno username valido." };
      }
      await sendFriendRequest(username.trim());
      return {
        intent,
        success: true,
        message: "Richiesta inviata. Sarà visibile tra gli amici solo dopo l'accettazione.",
      };
    }
    if (intent === "remove-friend") {
      const userId = formData.get("userId");
      if (typeof userId !== "string" || !userId) {
        return { intent, success: false, message: "Amico non valido." };
      }
      await removeFriend(userId);
      return { intent, success: true, message: "Amicizia rimossa." };
    }
    if (
      intent === "accept-friend-request" ||
      intent === "decline-friend-request" ||
      intent === "cancel-friend-request"
    ) {
      const requestId = formData.get("requestId");
      if (typeof requestId !== "string" || !requestId) {
        return { intent, success: false, message: "Richiesta di amicizia non valida." };
      }
      if (intent === "accept-friend-request") {
        await acceptFriendRequest(requestId);
        return { intent, success: true, message: "Richiesta accettata. Ora siete amici." };
      }
      if (intent === "decline-friend-request") {
        await declineFriendRequest(requestId);
        return { intent, success: true, message: "Richiesta rifiutata." };
      }
      await cancelFriendRequest(requestId);
      return { intent, success: true, message: "Richiesta annullata." };
    }
    return { intent, success: false, message: "Operazione non riconosciuta." };
  } catch (reason) {
    return actionError(intent, reason);
  }
}

export async function adminAction({ request }: ActionFunctionArgs): Promise<ActionResult> {
  const formData = await request.formData();
  const intent = actionIntent(formData);
  try {
    const account = await getSession(request.signal);
    if (!account.isAdmin || account.forcePasswordChange) {
      return { intent, success: false, status: 403, message: "Account non autorizzato o cambio password richiesto." };
    }
    switch (intent) {
      case "save-booking-policy": {
        const payload = jsonField(formData, "payload");
        if (
          !payload ||
          !Number.isInteger(payload.revision) ||
          !Number.isInteger(payload.minimumCancellationDays) ||
          !Array.isArray(payload.noShowRules)
        ) {
          return { intent, success: false, message: "Le impostazioni delle prenotazioni non sono valide." };
        }
        const rules = payload.noShowRules;
        if (rules.some((item) =>
          !isRecord(item) ||
          typeof item.id !== "string" ||
          typeof item.label !== "string" ||
          !Number.isInteger(item.occurrenceThreshold) ||
          !Number.isInteger(item.windowDays)
        )) {
          return { intent, success: false, message: "Controlla soglie, periodi e descrizioni degli avvisi." };
        }
        await saveBookingPolicy({
          revision: payload.revision as number,
          minimumCancellationDays: payload.minimumCancellationDays as number,
          noShowRules: rules as BookingPolicy["noShowRules"],
        });
        return { intent, success: true, message: "Impostazioni delle prenotazioni salvate." };
      }
      case "save-config": {
        const payload = jsonField(formData, "payload");
        if (
          !payload ||
          typeof payload.name !== "string" ||
          typeof payload.timeZone !== "string" ||
          !Array.isArray(payload.openingDays) ||
          (payload.sessionMode !== 0 && payload.sessionMode !== 1 && payload.sessionMode !== 2 && payload.sessionMode !== 3) ||
          (payload.sessionMode === 3 && payload.hourlyMinutes !== 60 && payload.hourlyMinutes !== 120)
        ) {
          return { intent, success: false, message: "La configurazione del campo non è valida." };
        }
        const value: RangeConfig = {
          id: String(payload.id ?? ""),
          creationDateTime: String(payload.creationDateTime ?? ""),
          name: payload.name,
          timeZone: payload.timeZone,
          sessionMode: payload.sessionMode,
          openingDays: payload.openingDays.filter((day): day is number => typeof day === "number"),
          morningStart: String(payload.morningStart ?? ""),
          morningEnd: String(payload.morningEnd ?? ""),
          afternoonStart: String(payload.afternoonStart ?? ""),
          afternoonEnd: String(payload.afternoonEnd ?? ""),
          hourlyMinutes: payload.sessionMode === 3 ? Number(payload.hourlyMinutes) : 60,
          continuousStart: typeof payload.continuousStart === "string" ? payload.continuousStart : null,
          continuousEnd: typeof payload.continuousEnd === "string" ? payload.continuousEnd : null,
          reservedReleaseDaysBefore: Number(payload.reservedReleaseDaysBefore),
        };
        await saveConfig(value);
        return { intent, success: true, message: "Configurazione salvata." };
      }
      case "save-bay": {
        const payload = jsonField(formData, "payload");
        const validCount = (value: unknown): value is number =>
          typeof value === "number" && Number.isInteger(value) && value >= 0;
        const validDimension = (value: unknown): value is number | null =>
          value === null || typeof value === "number" && Number.isFinite(value) && value > 0;
        const targets = payload?.targets;
        const partitions = payload?.partitions;
        const plates = payload?.plates;
        const pepper = payload?.pepper;
        const height = payload?.height;
        const width = payload?.width;
        if (
          !payload ||
          typeof payload.id !== "string" ||
          typeof payload.code !== "string" ||
          !Array.isArray(payload.mechanisms) ||
          !Array.isArray(payload.whitelistUserIds) ||
          !validCount(targets) ||
          !validCount(partitions) ||
          !validCount(plates) ||
          !validCount(pepper) ||
          !validDimension(height) ||
          !validDimension(width) ||
          (height === null) !== (width === null)
        ) {
          return { intent, success: false, message: "La configurazione della bay non è valida." };
        }
        const value: Bay = {
          id: payload.id,
          creationDateTime: String(payload.creationDateTime ?? ""),
          code: payload.code,
          description: String(payload.description ?? ""),
          targets,
          mechanisms: payload.mechanisms.filter(isRecord).map((mechanism) => ({
            type: typeof mechanism.type === "string" ? mechanism.type : "",
            quantity: typeof mechanism.quantity === "number" ? mechanism.quantity : 0,
          })),
          partitions,
          plates,
          pepper,
          height,
          width,
          status: Number(payload.status),
          reservedUntilUtc: typeof payload.reservedUntilUtc === "string" ? payload.reservedUntilUtc : null,
          whitelistUserIds: payload.whitelistUserIds.filter((id): id is string => typeof id === "string"),
        };
        await saveBay(value);
        return { intent, success: true, message: "Bay salvata.", entityId: value.id };
      }
      case "create-mechanism-type": {
        const payload = jsonField(formData, "payload");
        if (!payload || typeof payload.name !== "string" || !payload.name.trim()) {
          return { intent, success: false, message: "Inserisci il nome del meccanismo." };
        }
        await createMechanismType(payload.name);
        return { intent, success: true, message: "Tipo di meccanismo creato." };
      }
      case "add-closure": {
        const payload = jsonField(formData, "payload");
        if (!payload || typeof payload.localDate !== "string" || typeof payload.reason !== "string") {
          return { intent, success: false, message: "La chiusura del campo non è valida." };
        }
        await addClosure({
          localDate: payload.localDate,
          bayId: typeof payload.bayId === "string" ? payload.bayId : null,
          isClosed: true,
          reason: payload.reason,
        });
        return { intent, success: true, message: "Chiusura salvata." };
      }
      case "save-closure": {
        const payload = jsonField(formData, "payload");
        if (!payload || typeof payload.id !== "string" || typeof payload.localDate !== "string" || typeof payload.isClosed !== "boolean") {
          return { intent, success: false, message: "La chiusura del campo non è valida." };
        }
        const value: Closure = {
          id: payload.id,
          creationDateTime: String(payload.creationDateTime ?? ""),
          localDate: payload.localDate,
          bayId: typeof payload.bayId === "string" ? payload.bayId : null,
          isClosed: payload.isClosed,
          reason: String(payload.reason ?? ""),
        };
        await saveClosure(value);
        return { intent, success: true, message: "Chiusura rimossa." };
      }
      case "approve-user": {
        const id = jsonField(formData, "payload")?.id;
        if (typeof id !== "string" || !id) return { intent, success: false, message: "Utente non valido." };
        await approveUser(id);
        return { intent, success: true, message: "Account approvato." };
      }
      case "decide-booking": {
        const payload = jsonField(formData, "payload");
        const id = payload?.id;
        const approved = payload?.approved;
        if (typeof id !== "string" || !id || typeof approved !== "boolean") {
          return { intent, success: false, message: "Decisione di prenotazione non valida." };
        }
        await decideBooking(id, approved);
        return { intent, success: true, message: approved ? "Prenotazione approvata." : "Prenotazione rifiutata." };
      }
      case "decide-booking-attendance": {
        const payload = jsonField(formData, "payload");
        if (typeof payload?.id !== "string" || typeof payload.isNoShow !== "boolean") {
          return { intent, success: false, message: "Indicazione di presenza non valida." };
        }
        await setBookingAttendance(payload.id, payload.isNoShow);
        return {
          intent,
          success: true,
          message: payload.isNoShow ? "Mancata presenza registrata." : "Indicazione di mancata presenza rimossa.",
        };
      }
      case "cancel-admin-booking": {
        const payload = jsonField(formData, "payload");
        if (
          typeof payload?.id !== "string" ||
          !Number.isInteger(payload.policyRevision) ||
          typeof payload.confirmExistingNoShowRemoval !== "boolean" ||
          typeof payload.expectedIsNoShow !== "boolean" ||
          payload.expectedNoShowDate !== null && typeof payload.expectedNoShowDate !== "string" ||
          payload.recordNoShow !== null && typeof payload.recordNoShow !== "boolean"
        ) {
          return { intent, success: false, message: "Conferma di cancellazione non valida." };
        }
        await adminCancelBooking(payload.id, {
          policyRevision: payload.policyRevision as number,
          recordNoShow: payload.recordNoShow as boolean | null,
          confirmExistingNoShowRemoval: payload.confirmExistingNoShowRemoval,
          expectedIsNoShow: payload.expectedIsNoShow,
          expectedNoShowDate: payload.expectedNoShowDate as string | null,
        });
        return { intent, success: true, message: "Prenotazione annullata." };
      }
      case "create-admin-booking": {
        const payload = jsonField(formData, "payload");
        const userId = payload?.userId;
        const bayId = payload?.bayId;
        const localDate = payload?.localDate;
        const periodKey = payload?.periodKey;
        const requestText = payload?.request;
        const expectedStartUtc = payload?.expectedStartUtc;
        const expectedEndUtc = payload?.expectedEndUtc;
        if (
          typeof userId !== "string" || !userId ||
          typeof bayId !== "string" || !bayId ||
          typeof localDate !== "string" || !localDate ||
          typeof periodKey !== "string" || !periodKey ||
          typeof requestText !== "string" || requestText.length > 1000 ||
          typeof expectedStartUtc !== "string" || !expectedStartUtc ||
          typeof expectedEndUtc !== "string" || !expectedEndUtc
        ) {
          return { intent, success: false, message: "Completa i dati della prenotazione." };
        }
        const offered = await sessions(localDate, request.signal);
        const selected = offered.sessions.find((item) => item.periodKey === periodKey);
        if (!selected || selected.startUtc !== expectedStartUtc || selected.endUtc !== expectedEndUtc) {
          return { intent, success: false, message: "Gli orari sono cambiati. Seleziona un intervallo aggiornato." };
        }
        await createAdminBooking({
          userId,
          bayId,
          localDate,
          periodKey,
          request: requestText,
          expectedStartUtc,
          expectedEndUtc,
        });
        return { intent, success: true, message: "Prenotazione creata e approvata." };
      }
      default:
        return { intent, success: false, message: "Operazione non riconosciuta." };
    }
  } catch (reason) {
    return actionError(intent, reason);
  }
}
