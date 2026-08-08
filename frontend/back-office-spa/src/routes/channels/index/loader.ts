import { get, endpoints } from '@morwalpizvideo/services';
import type { Channel } from '@morwalpizvideo/models';
import { requireChannelPayload } from '../response';

export default async function loader() {
  const response = await get(endpoints.CHANNELS);
  const channels = requireChannelPayload<unknown>(response, 'Unable to load accessible channels');
  if (!Array.isArray(channels)) {
    throw new Response('Unable to load accessible channels', { status: 502 });
  }

  return channels as Channel[];
}
