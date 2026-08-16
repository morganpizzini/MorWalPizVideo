export interface PagesApiError {
  errors: string[];
  status?: number;
}

export function getPagesApiError(response: unknown): PagesApiError | null {
  if (!response || typeof response !== 'object') return null;
  const value = response as { errors?: unknown; status?: unknown };
  if (!Array.isArray(value.errors)) return null;
  return {
    errors: value.errors.map(String),
    status: typeof value.status === 'number' ? value.status : undefined,
  };
}

export function getPagesErrorMessages(response: unknown, fallback: string): string[] {
  return getPagesApiError(response)?.errors ?? [fallback];
}

export function throwIfPagesApiError(response: unknown, fallback: string): void {
  const error = getPagesApiError(response);
  if (error) throw new Response(error.errors.join('\n') || fallback, { status: error.status ?? 500 });
}