export type Account = { id: string; username: string; firstName: string; lastName: string; status: string; isAdmin: boolean; forcePasswordChange: boolean };
export type Bay = { id: string; code: string; description: string; mechanisms: { type: string; quantity: number }[]; partitions: number; plates: number; pepper: boolean };
export type Availability = { localDate: string; periodKey: string; startUtc: string; endUtc: string; bays: Bay[] };
let csrf = '';
const apiBaseUrl = typeof window !== 'undefined' ? window.ENV?.API_BASE_URL ?? import.meta.env.VITE_API_BASE_URL ?? '' : import.meta.env.VITE_API_BASE_URL ?? '';
function apiUrl(path: string) { return apiBaseUrl ? new URL(path, apiBaseUrl).toString() : path; }
async function request<T>(path: string, init: RequestInit = {}): Promise<T> { const response = await fetch(apiUrl(path), { ...init, credentials: 'include', headers: { 'Content-Type': 'application/json', ...(init.method && init.method !== 'GET' ? { 'X-CSRF-TOKEN': csrf } : {}), ...init.headers } }); if (!response.ok) { const body = await response.json().catch(() => ({})); throw new Error(body.message ?? `HTTP ${response.status}`); } return response.status === 204 ? undefined as T : response.json(); }
export async function getCsrf() { const result = await request<{ token: string }>('/api/shooting-range/csrf'); csrf = result.token; }
export const register = (body: object) => request<Account>('/api/shooting-range/auth/register', { method: 'POST', body: JSON.stringify(body) });
export const login = async (body: object) => { await getCsrf(); return request<Account>('/api/shooting-range/auth/login', { method: 'POST', body: JSON.stringify(body) }); };
export const availability = (date: string, period: string) => request<Availability>(`/api/shooting-range/availability?date=${date}&period=${encodeURIComponent(period)}`);
export const book = (body: object) => request('/api/shooting-range/bookings', { method: 'POST', body: JSON.stringify(body) });
export const myBookings = () => request<unknown[]>('/api/shooting-range/bookings/mine');
export const messages = () => request<unknown[]>('/api/shooting-range/messages');
export const openMessage = (body: object) => request('/api/shooting-range/messages', { method: 'POST', body: JSON.stringify(body) });
export const adminUsers = () => request<unknown[]>('/api/shooting-range/admin/users');
