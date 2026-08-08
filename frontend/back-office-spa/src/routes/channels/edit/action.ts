import { ActionFunctionArgs, data } from 'react-router';
import { put, endpoints, ComposeUrl } from '@morwalpizvideo/services';
import { UpdateChannelDTO } from '@/models';
import { channelActionError, getChannelApiError } from '../response';

export default async function action({ request, params }: ActionFunctionArgs) {
  const values = Object.fromEntries(await request.formData()) as UpdateChannelDTO;
  const errors: Record<string, string | string[]> = {};

  // Validate fields
  if (!values.channelName || values.channelName.trim().length === 0) {
    errors['channelName'] = 'Channel name cannot be empty';
  }

  // Check for errors
  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  try {
    const response = await put(ComposeUrl(endpoints.CHANNELS_DETAIL, { channelId: params.id! }), values);
    if (getChannelApiError(response)) {
      return channelActionError(response, 'Unable to update channel');
    }

    return data({ success: true }, { status: 200 });
  } catch (error) {
    return channelActionError(error, 'Unable to update channel');
  }
}
