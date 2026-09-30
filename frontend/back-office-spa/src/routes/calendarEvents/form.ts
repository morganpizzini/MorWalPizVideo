import { data } from 'react-router';
import { ApiResponseError } from '@morwalpizvideo/services';
import type { SaveCalendarEventRequest } from '@morwalpizvideo/models';

export interface CalendarActionResult {
  success: boolean;
  conflict?: boolean;
  updatedTitle?: string;
  errors?: { generics?: string[]; fields?: Record<string, string[]> };
}

export function calendarPayload(form: FormData, editing: boolean): SaveCalendarEventRequest {
  const categories: unknown = JSON.parse(String(form.get('categories') ?? '[]'));
  if (
    !Array.isArray(categories) ||
    categories.some(
      category => !category || typeof category.id !== 'string' || typeof category.title !== 'string'
    )
  )
    throw new ApiResponseError({ status: 400, errors: ['Invalid categories.'] });
  return {
    ...(editing ? { id: String(form.get('id') ?? ''), revision: calendarRevision(form) } : {}),
    title: String(form.get(editing ? 'newTitle' : 'title') ?? ''),
    description: String(form.get('description') ?? ''),
    startDate: String(form.get('startDate') ?? ''),
    endDate: String(form.get('endDate') ?? ''),
    categories: categories.map(category => ({ id: category.id, title: category.title })),
    matchId: String(form.get('matchId') ?? ''),
  };
}

export function calendarRevision(form: FormData): number {
  const raw = form.get('revision');
  const revision = raw === null || raw === '' ? NaN : Number(raw);
  if (!Number.isSafeInteger(revision) || revision < 0)
    throw new ApiResponseError({
      status: 400,
      errors: ['A valid Calendar revision is required. Reload the event.'],
    });
  return revision;
}

export function calendarActionError(error: unknown) {
  const apiError = error instanceof ApiResponseError ? error : undefined;
  const fields =
    apiError?.fieldErrors &&
    Object.fromEntries(
      Object.entries(apiError.fieldErrors).map(([field, messages]) => [
        field.charAt(0).toLowerCase() + field.slice(1),
        (Array.isArray(messages) ? messages : [messages]).map(String),
      ])
    );
  return data<CalendarActionResult>(
    {
      success: false,
      conflict: apiError?.status === 409,
      errors: {
        generics: [error instanceof Error ? error.message : 'Calendar request failed.'],
        fields,
      },
    },
    { status: apiError?.status ?? (error instanceof SyntaxError ? 400 : 500) }
  );
}
