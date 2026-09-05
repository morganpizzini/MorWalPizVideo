import { fetchNewsletters } from '@morwalpizvideo/services';

export default async function loader() {
  const response = await fetchNewsletters();
  if (!Array.isArray(response)) throw new Response('Unable to load newsletters', { status: 502 });
  return response;
}