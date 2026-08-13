import { data } from 'react-router';

interface QuickLinksApiError {
  errors: unknown[];
  status?: number;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function formatError(error: unknown): string {
  if (typeof error === 'string') return error;
  if (isRecord(error) && typeof error.message === 'string') return error.message;
  try {
    return JSON.stringify(error) ?? String(error);
  } catch {
    return String(error);
  }
}

export function getQuickLinksApiError(response: unknown): QuickLinksApiError | null {
  if (!isRecord(response) || !Array.isArray(response.errors)) return null;
  return {
    errors: response.errors,
    status: typeof response.status === 'number' ? response.status : undefined,
  };
}

export function getQuickLinksErrorMessages(response: unknown, fallback: string): string[] {
  const apiError = getQuickLinksApiError(response);
  const messages = apiError?.errors.map(formatError).filter(Boolean) ?? [];
  return messages.length > 0 ? messages : [fallback];
}

export function throwIfQuickLinksApiError(response: unknown, fallback: string): void {
  const apiError = getQuickLinksApiError(response);
  if (!apiError) return;

  throw new Response(getQuickLinksErrorMessages(response, fallback).join('\n'), {
    status: apiError.status ?? 500,
  });
}

export function quickLinksActionError(response: unknown, fallback: string) {
  const apiError = getQuickLinksApiError(response);
  return data(
    { success: false, errors: { generics: getQuickLinksErrorMessages(response, fallback) } },
    { status: apiError?.status ?? 500 },
  );
}