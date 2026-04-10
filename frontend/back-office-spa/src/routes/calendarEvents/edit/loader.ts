import { get, endpoints, ComposeUrl } from '@morwalpizvideo/services';

export default async function loader({ params }: { params: { title: string } }) {
  const title = decodeURIComponent(params.title);
  try {
    const response = await get(ComposeUrl(endpoints.CALENDAREVENTS_DETAIL, { title: encodeURIComponent(title) }));
    return response;
  } catch (error) {
    throw new Response('Calendar event not found', { status: 404 });
  }
}
