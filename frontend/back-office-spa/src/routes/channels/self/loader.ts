import { get, endpoints, ComposeUrl, getSelectedChannelId } from '@morwalpizvideo/services';
import type { Channel } from '@morwalpizvideo/models';
import { requireChannelPayload } from '../response';

export default async function loader() {
  const channelId = getSelectedChannelId();
  if (!channelId) {
    throw new Response('No channel is selected', { status: 404 });
  }

  const response = await get(ComposeUrl(endpoints.CHANNELS_DETAIL, { channelId }));
  return requireChannelPayload<Channel>(response, 'Channel not found');
}