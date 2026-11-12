import { data } from 'react-router';
import { ApiResponseError, getApiFieldErrors } from '@morwalpizvideo/services';

interface ChannelApiErrorPayload {
  errors?: unknown[];
  fieldErrors?: Record<string, unknown>;
  status?: number;
  channelContextError?: boolean;
}

export interface ChannelActionErrors {
  generics: string[];
  channelName?: string;
  yTChannelId?: string;
  shortLinkUrl?: string;
  publicBaseUrl?: string;
  googleAnalyticsUrl?: string;
  socials?: string;
  socialPublishing?: string;
}

export interface ChannelActionError {
  success: false;
  errors: ChannelActionErrors;
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

function toFieldErrors(value: unknown): Record<string, string[]> {
  if (!isRecord(value)) return {};
  return Object.fromEntries(
    Object.entries(value).map(([field, messages]) => [
      field,
      (Array.isArray(messages) ? messages : [messages]).map(formatError).filter(Boolean),
    ])
  );
}

export function getChannelApiError(response: unknown): ChannelApiErrorPayload | null {
  if (typeof ApiResponseError === 'function' && response instanceof ApiResponseError) {
    return {
      errors: response.errors,
      fieldErrors: getApiFieldErrors(response),
      status: response.status,
    };
  }
  if (!isRecord(response)) return null;

  const fieldErrors = isRecord(response.fieldErrors)
    ? response.fieldErrors
    : isRecord(response.errors)
      ? response.errors
      : undefined;
  if (!Array.isArray(response.errors) && !fieldErrors && typeof response.status !== 'number') {
    return null;
  }

  return {
    errors: Array.isArray(response.errors) ? response.errors : [],
    fieldErrors,
    status: typeof response.status === 'number' ? response.status : undefined,
    channelContextError: response.channelContextError === true,
  };
}

function channelFieldName(field: string): keyof Omit<ChannelActionErrors, 'generics'> | null {
  const normalized = field
    .replace(/\[(\d+)\]/g, '.$1')
    .split('.')
    .filter(Boolean);
  const root = normalized[0]?.toLowerCase();

  switch (root) {
    case 'channelname':
      return 'channelName';
    case 'ytchannelid':
    case 'youtubechannelid':
      return 'yTChannelId';
    case 'shortlinkurl':
      return 'shortLinkUrl';
    case 'publicbaseurl':
      return 'publicBaseUrl';
    case 'googleanalyticsurl':
      return 'googleAnalyticsUrl';
    case 'socials':
      return 'socials';
    case 'socialpublishing':
      return 'socialPublishing';
    default:
      return null;
  }
}

function mappedErrors(fieldErrors: Record<string, string[]>) {
  const mapped: Partial<Record<keyof Omit<ChannelActionErrors, 'generics'>, string[]>> = {};
  const generics: string[] = [];

  for (const [field, messages] of Object.entries(fieldErrors)) {
    const target = channelFieldName(field);
    if (!target) {
      generics.push(...messages.map(message => `${field}: ${message}`));
      continue;
    }

    mapped[target] ??= [];
    mapped[target]!.push(...messages);
  }

  return { mapped, generics };
}

export function throwIfChannelApiError(response: unknown, fallbackMessage: string): void {
  const apiError = getChannelApiError(response);
  if (!apiError) {
    return;
  }

  const fields = mappedErrors(toFieldErrors(apiError.fieldErrors));
  const messages = (apiError.errors ?? []).map(formatError).filter(Boolean);
  throw new Response(
    [...fields.generics, ...messages].join('\n') ||
      Object.values(fields.mapped).flat().join('\n') ||
      fallbackMessage,
    { status: apiError.status ?? 500 }
  );
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
  const fieldErrors = toFieldErrors(apiError?.fieldErrors);
  const fields = mappedErrors(fieldErrors);
  const messages = (apiError?.errors ?? []).map(formatError).filter(Boolean);
  const nonValidationTitle = messages.filter(
    message => !(Object.keys(fieldErrors).length > 0 &&
      message === 'One or more validation errors occurred.')
  );
  const errors: ChannelActionErrors = {
    generics: [...fields.generics, ...nonValidationTitle],
  };

  for (const [field, fieldMessages] of Object.entries(fields.mapped)) {
    if (fieldMessages?.length) {
      errors[field as keyof Omit<ChannelActionErrors, 'generics'>] = fieldMessages.join('; ');
    }
  }

  if (errors.generics.length === 0 && Object.keys(fields.mapped).length === 0) {
    const fallback = response instanceof Error ? response.message : fallbackMessage;
    errors.generics = [fallback || fallbackMessage];
  }

  const error: ChannelActionError = {
    success: false,
    errors,
    ...(apiError?.channelContextError ? { channelContextError: true } : {}),
  };

  return data(error, { status: apiError?.status ?? 500 });
}

export interface ChannelMutationResult {
  success?: true;
  cacheInvalidation?: {
    status?: string;
    warningCode?: string | null;
    message?: string | null;
  };
  channel?: unknown;
}

export function hasCacheInvalidationWarning(result: unknown): result is ChannelMutationResult {
  return isRecord(result) &&
    isRecord(result.cacheInvalidation) &&
    result.cacheInvalidation.status === 'failed';
}
