import { data } from 'react-router';

interface ChannelApiErrorPayload {
  errors?: unknown[];
  status?: number;
  channelContextError?: boolean;
}

export interface ChannelActionError {
  success: false;
  errors: {
    generics: string[];
  };
  channelContextError?: boolean;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function formatError(error: unknown): string {
  if (typeof error === 'string') {
    return error;
  }

  if (isRecord(error) && typeof error.message === 'string') {
    return error.message;
  }

  try {
    return JSON.stringify(error);
  } catch {
    return String(error);
  }
}

export function getChannelApiError(response: unknown): ChannelApiErrorPayload | null {
  if (!isRecord(response) || !Array.isArray(response.errors)) {
    return null;
  }

  const status = typeof response.status === 'number' ? response.status : undefined;
  return {
    errors: response.errors,
    status,
    channelContextError: response.channelContextError === true,
  };
}

export function throwIfChannelApiError(response: unknown, fallbackMessage: string): void {
  const apiError = getChannelApiError(response);
  if (!apiError) {
    return;
  }

  const messages = (apiError.errors ?? []).map(formatError).filter(Boolean);
  throw new Response(messages.join('\n') || fallbackMessage, {
    status: apiError.status ?? 500,
  });
}

export function requireChannelPayload<T>(response: unknown, fallbackMessage: string): T {
  throwIfChannelApiError(response, fallbackMessage);
  if (response === null || response === undefined) {
    throw new Response(fallbackMessage, { status: 502 });
  }

  return response as T;
}

export function channelActionError(response: unknown, fallbackMessage: string) {
  const apiError = getChannelApiError(response);
  const messages = (apiError?.errors ?? []).map(formatError).filter(Boolean);
  const error: ChannelActionError = {
    success: false,
    errors: {
      generics: messages.length > 0 ? messages : [fallbackMessage],
    },
    ...(apiError?.channelContextError ? { channelContextError: true } : {}),
  };

  return data(error, { status: apiError?.status ?? 500 });
}