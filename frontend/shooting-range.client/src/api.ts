export type Account = {
  id: string;
  username: string;
  firstName: string;
  lastName: string;
  status: number;
  isAdmin: boolean;
  forcePasswordChange: boolean;
};
export type AdminUser = Account & { creationDateTime: string };
export type Bay = {
  id: string;
  creationDateTime: string;
  code: string;
  description: string;
  mechanisms: { type: string; quantity: number }[];
  targets: number;
  partitions: number;
  plates: number;
  pepper: number;
  height: number | null;
  width: number | null;
  status: number;
  reservedUntilUtc: string | null;
  whitelistUserIds: string[];
};
export type MechanismType = { id: string; name: string };
export type Availability = {
  localDate: string;
  periodKey: string;
  startUtc: string;
  endUtc: string;
  bays: Bay[];
};
export type Session = { periodKey: string; startUtc: string; endUtc: string };
export type Sessions = {
  localDate: string;
  timeZone: string;
  sessions: Session[];
};
export type RangeConfig = {
  id: string;
  creationDateTime: string;
  name: string;
  timeZone: string;
  sessionMode: 0 | 1 | 2;
  openingDays: number[];
  morningStart: string;
  morningEnd: string;
  afternoonStart: string;
  afternoonEnd: string;
  hourlyMinutes: number;
  continuousStart: string | null;
  continuousEnd: string | null;
  reservedReleaseDaysBefore: number;
};
export type Closure = {
  id: string;
  creationDateTime: string;
  localDate: string;
  bayId: string | null;
  isClosed: boolean;
  reason: string;
};
export type Booking = Session & {
  id: string;
  creationDateTime: string;
  userId: string;
  bayId: string;
  localDate: string;
  request: string;
  status: number;
};
export type Message = {
  authorUserId: string;
  fromAdmin: boolean;
  text: string;
  createdUtc: string;
};
export type MessageThread = {
  id: string;
  creationDateTime: string;
  userId: string;
  subject: string;
  status?: number;
  messages?: Message[] | null;
};
export type MessageStatusDecision = 1 | 2 | 3;
export type BookingRequest = {
  bayId: string;
  localDate: string;
  periodKey: string;
  request: string;
  expectedStartUtc?: string;
  expectedEndUtc?: string;
};
const prefix = "/api/shooting-range";
const apiBaseUrl =
  typeof window !== "undefined"
    ? (window.ENV?.API_BASE_URL ?? import.meta.env.VITE_API_BASE_URL ?? "")
    : (import.meta.env.VITE_API_BASE_URL ?? "");
function apiUrl(path: string) {
  return apiBaseUrl ? new URL(path, apiBaseUrl).toString() : path;
}

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message);
  }
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set("Content-Type", "application/json");
  if (
    init.method &&
    init.method !== "GET" &&
    !path.includes("/auth/register")
  ) {
    headers.set("X-CSRF-TOKEN", await getCsrf());
  }
  const response = await fetch(apiUrl(path), {
    ...init,
    credentials: "include",
    headers,
  });
  if (!response.ok) {
    if (
      response.status === 401 &&
      typeof window !== "undefined" &&
      !path.includes("/auth/")
    )
      window.dispatchEvent(new Event("range-session-expired"));
    const body = await response.json().catch(() => ({}));
    throw new ApiError(
      response.status,
      body.message ?? body.title ?? `HTTP ${response.status}`,
    );
  }
  return response.status === 204 ? (undefined as T) : response.json();
}

export async function getCsrf(): Promise<string> {
  return (await request<{ token: string }>(`${prefix}/csrf`)).token;
}
export const register = (body: {
  username: string;
  password: string;
  firstName: string;
  lastName: string;
}) =>
  request<Account>(`${prefix}/auth/register`, {
    method: "POST",
    body: JSON.stringify(body),
  });
export async function login(body: {
  username: string;
  password: string;
}): Promise<Account> {
  const account = await request<Account>(`${prefix}/auth/login`, {
    method: "POST",
    body: JSON.stringify(body),
  });
  await getCsrf();
  return account;
}
export async function changePassword(body: {
  currentPassword: string;
  newPassword: string;
}): Promise<void> {
  await request<void>(`${prefix}/auth/password`, {
    method: "POST",
    body: JSON.stringify(body),
  });
  await getCsrf();
}
export const getSession = (signal?: AbortSignal) =>
  request<Account>(`${prefix}/auth/session`, { signal });
export const logout = () =>
  request<void>(`${prefix}/auth/logout`, { method: "POST" });
export const sessions = (date: string, signal?: AbortSignal) =>
  request<Sessions>(`${prefix}/sessions?date=${encodeURIComponent(date)}`, {
    signal,
  });
export const getConfig = (signal?: AbortSignal) =>
  request<RangeConfig>(`${prefix}/config`, { signal });
export const availability = (
  date: string,
  period: string,
  signal?: AbortSignal,
) =>
  request<Availability>(
    `${prefix}/availability?date=${encodeURIComponent(date)}&period=${encodeURIComponent(period)}`,
    { signal },
  );
export const book = (body: BookingRequest) =>
  request<Booking>(`${prefix}/bookings`, {
    method: "POST",
    body: JSON.stringify(body),
  });
export const myBookings = (signal?: AbortSignal) =>
  request<Booking[]>(`${prefix}/bookings/mine`, { signal });
export const cancelBooking = (id: string) =>
  request<void>(`${prefix}/bookings/${encodeURIComponent(id)}/cancel`, {
    method: "POST",
  });
export const messages = (signal?: AbortSignal) =>
  request<MessageThread[]>(`${prefix}/messages`, { signal });
export const openMessage = (body: { subject: string; text: string }) =>
  request<MessageThread>(`${prefix}/messages`, { method: "POST", body: JSON.stringify(body) });
export const decideMessageStatus = (id: string, status: MessageStatusDecision) =>
  request<MessageThread>(`${prefix}/admin/messages/${encodeURIComponent(id)}/status`, {
    method: "POST",
    body: JSON.stringify({ status }),
  });
export const adminUsers = (signal?: AbortSignal) =>
  request<AdminUser[]>(`${prefix}/admin/users`, { signal });
export const adminConfig = (signal?: AbortSignal) =>
  request<RangeConfig>(`${prefix}/admin/config`, { signal });
export const saveConfig = (body: RangeConfig) =>
  request<RangeConfig>(`${prefix}/admin/config`, {
    method: "PUT",
    body: JSON.stringify(body),
  });
export const adminBays = (signal?: AbortSignal) =>
  request<Bay[]>(`${prefix}/admin/bays`, { signal });
export const adminMechanismTypes = (signal?: AbortSignal) =>
  request<MechanismType[]>(`${prefix}/admin/mechanism-types`, { signal });
export const createMechanismType = (name: string) =>
  request<MechanismType>(`${prefix}/admin/mechanism-types`, {
    method: "POST",
    body: JSON.stringify({ name }),
  });
export const saveBay = (body: Bay) =>
  request<Bay>(`${prefix}/admin/bays/${encodeURIComponent(body.id)}`, {
    method: "PUT",
    body: JSON.stringify(body),
  });
export const adminClosures = (signal?: AbortSignal) =>
  request<Closure[]>(`${prefix}/admin/exceptions`, { signal });
export const addClosure = (body: {
  localDate: string;
  bayId: string | null;
  isClosed: boolean;
  reason: string;
}) =>
  request<Closure>(`${prefix}/admin/exceptions`, {
    method: "POST",
    body: JSON.stringify(body),
  });
export const saveClosure = (body: Closure) =>
  request<Closure>(
    `${prefix}/admin/exceptions/${encodeURIComponent(body.id)}`,
    { method: "PUT", body: JSON.stringify(body) },
  );
export const adminBookings = (signal?: AbortSignal) =>
  request<Booking[]>(`${prefix}/admin/bookings`, { signal });
export const adminPendingBookingCount = (signal?: AbortSignal) =>
  request<{ count: number }>(`${prefix}/admin/bookings/pending-count`, {
    signal,
  });
export const decideBooking = (id: string, approved: boolean) =>
  request<void>(`${prefix}/admin/bookings/${encodeURIComponent(id)}/decision`, {
    method: "POST",
    body: JSON.stringify({ approved }),
  });
export const approveUser = (id: string) =>
  request<void>(`${prefix}/admin/users/${encodeURIComponent(id)}/approve`, {
    method: "POST",
  });
