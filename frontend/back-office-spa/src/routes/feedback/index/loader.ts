import type { LoaderFunctionArgs } from 'react-router';
import type { FeedbackStatus } from '@morwalpizvideo/models';
import { getFeedbackPage } from '@morwalpizvideo/services';

export default async function loader({ request }: LoaderFunctionArgs) {
  const url = new URL(request.url);
  const page = Math.max(1, Number(url.searchParams.get('page') ?? 1));
  const pageSize = Math.min(50, Math.max(1, Number(url.searchParams.get('pageSize') ?? 20)));

  if (!Number.isInteger(page) || !Number.isInteger(pageSize)) {
    throw new Response('Invalid pagination', { status: 400 });
  }

  return getFeedbackPage(
    (url.searchParams.get('status') as FeedbackStatus | null) || undefined,
    url.searchParams.get('application') || undefined,
    page,
    pageSize,
  );
}
