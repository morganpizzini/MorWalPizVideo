import { data } from 'react-router';
import { Delete, endpoints, ComposeUrl } from '@morwalpizvideo/services';
import { channelActionError, getChannelApiError } from '../response';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());

  try {
    const response = await Delete(ComposeUrl(endpoints.CHANNELS_DETAIL, { channelId: values.id as string }));
    if (getChannelApiError(response)) {
      return channelActionError(response, 'Unable to delete channel');
    }

    return data({ success: true }, { status: 200 });
  } catch (error) {
    return channelActionError(error, 'Unable to delete channel');
  }
}
