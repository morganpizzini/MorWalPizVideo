import { getSponsors } from '@services/sponsors';
import { getCustomFormByUrl } from '@services/customForms';
import { data } from 'react-router';

function isApiError(value: unknown): value is { errors: unknown[]; status?: number } {
  return (
    typeof value === 'object' &&
    value !== null &&
    Array.isArray((value as { errors?: unknown[] }).errors)
  );
}

function formatError(error: unknown): string {
  if (typeof error === 'string') return error;
  if (typeof error === 'object' && error !== null && 'message' in error) {
    const message = (error as { message?: unknown }).message;
    if (typeof message === 'string') return message;
  }
  return String(error);
}

export default async function loader() {
  const formUrl = 'sponsor';
  const [sponsors, formResponse] = await Promise.all([getSponsors(), getCustomFormByUrl(formUrl)]);
  if (!sponsors) {
    // throw to ErrorBoundary
    throw data(null, { status: 404 });
  }

  if (isApiError(formResponse)) {
    if (formResponse.status === 404) return { sponsors, form: undefined };

    const message =
      formResponse.errors.map(formatError).filter(Boolean).join('\n') || 'Sponsor form unavailable';
    throw new Response(message, { status: formResponse.status ?? 500 });
  }

  if (!formResponse) {
    // throw to ErrorBoundary
    throw data(null, { status: 404 });
  }

  return { sponsors, form: formResponse };
}
