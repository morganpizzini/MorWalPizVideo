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

export default async function loader() {
  const formUrl = 'sponsor';
  const [sponsors, formResponse] = await Promise.all([
    getSponsors(),
    (async () => {
      try {
        return await getCustomFormByUrl(formUrl);
      } catch {
        return undefined;
      }
    })(),
  ]);

  if (!sponsors) {
    // throw to ErrorBoundary
    throw data(null, { status: 404 });
  }

  if (isApiError(formResponse)) {
    if (formResponse.status === 404) return { sponsors, form: undefined };

    return { sponsors, form: undefined };
  }

  if (!formResponse) {
    return { sponsors, form: undefined };
  }

  return { sponsors, form: formResponse };
}
