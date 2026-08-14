import { fetchChannelNews } from '@morwalpizvideo/services';
import type { ChannelNewsAdmin } from '@morwalpizvideo/models';

export default async function loader(): Promise<ChannelNewsAdmin[]> {
  const response = await fetchChannelNews();
  if (!Array.isArray(response)) throw new Response('Unable to load ChannelNews', { status: 502 });
  return response;
}
