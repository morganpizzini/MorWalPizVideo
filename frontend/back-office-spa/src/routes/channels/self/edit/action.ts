import { getSelectedChannelId } from '@morwalpizvideo/services';
import channelFormAction from '../../form/action';

export default async function action(args: { request: Request }) {
  const channelId = getSelectedChannelId();
  if (!channelId) {
    return new Response('No channel is selected', { status: 404 });
  }

  return channelFormAction({ ...args, params: { id: channelId } } as never);
}