import { data } from 'react-router';
import { post, endpoints } from '@morwalpizvideo/services';
import { CreateChannelDTO } from '@morwalpizvideo/models';
import { channelActionError, getChannelApiError } from '../response';

export default async function action({ request }: { request: Request }) {
  const formData = await request.formData();
  const channelName = formData.get('channelName');
  const yTChannelId = formData.get('yTChannelId');
  const channelNameValue = typeof channelName === 'string' ? channelName.trim() : '';
  const yTChannelIdValue = typeof yTChannelId === 'string' ? yTChannelId.trim() : '';
  const errors: Record<string, string | string[]> = {};

  // Validate fields
  if (!channelNameValue) {
    errors['channelName'] = 'Channel name cannot be empty';
  }

  if (!yTChannelIdValue) {
    errors['yTChannelId'] = 'YouTube channel ID cannot be empty';
  }

  // Check for errors
  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  try {
    const payload: Pick<CreateChannelDTO, 'channelName' | 'yTChannelId'> = {
      channelName: channelNameValue,
      yTChannelId: yTChannelIdValue,
    };
    const response = await post(endpoints.CHANNELS, payload);
    if (getChannelApiError(response)) {
      return channelActionError(response, 'Unable to create channel');
    }

    return data({ success: true }, { status: 201 });
  } catch (error) {
    return channelActionError(error, 'Unable to create channel');
  }
}
