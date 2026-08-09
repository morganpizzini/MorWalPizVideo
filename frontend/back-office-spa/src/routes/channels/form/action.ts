import { ActionFunctionArgs, data } from 'react-router';
import { post, put, endpoints, ComposeUrl } from '@morwalpizvideo/services';
import { channelActionError, getChannelApiError } from '../response';

export default async function action({ request, params }: ActionFunctionArgs) {
  const values = Object.fromEntries(await request.formData());
  const errors: Record<string, string> = {};
  const { id } = params;
  const channelName = typeof values.channelName === 'string' ? values.channelName.trim() : '';
  const yTChannelId = typeof values.yTChannelId === 'string' ? values.yTChannelId.trim() : '';
  const socialProvider = typeof values.socialProvider === 'string' ? values.socialProvider.toLowerCase() : '';
  const socialHandler = typeof values.socialHandler === 'string' ? values.socialHandler.trim() : '';
  const socials = socialProvider && socialHandler ? [{ provider: socialProvider, handler: socialHandler }] : [];

  if (!channelName) {
    errors['channelName'] = 'Channel name cannot be empty';
  }

  if (!id && !yTChannelId) {
    errors['yTChannelId'] = 'YouTube Channel ID cannot be empty';
  }

  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  try {
    if (id) {
      const response = await put(ComposeUrl(endpoints.CHANNELS_DETAIL, { channelId: id }), {
        channelId: id,
        channelName,
        socials,
      });
      if (getChannelApiError(response)) {
        return channelActionError(response, 'Unable to update channel');
      }
    } else {
      const payload = {
        channelName,
        yTChannelId,
        socials,
      };
      const response = await post(endpoints.CHANNELS, payload);
      if (getChannelApiError(response)) {
        return channelActionError(response, 'Unable to create channel');
      }
    }
    return data({ success: true }, { status: id ? 200 : 201 });
  } catch (error) {
    return channelActionError(error, id ? 'Unable to update channel' : 'Unable to create channel');
  }
}
