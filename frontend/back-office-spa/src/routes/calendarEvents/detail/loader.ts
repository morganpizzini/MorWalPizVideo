import { getCalendarEventByTitle } from '@morwalpizvideo/services';
import type { LoaderFunctionArgs } from 'react-router';

export default async function loader({ params }: LoaderFunctionArgs) {
  if (!params.id) throw new Response('Calendar event title is required', { status: 400 });
  return getCalendarEventByTitle(params.id);
}
