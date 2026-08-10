import { ActionFunctionArgs, data } from 'react-router';
import { post, put, endpoints, ComposeUrl } from '@morwalpizvideo/services';
import { channelActionError, getChannelApiError } from '../response';

export default async function action({ request, params }: ActionFunctionArgs) {
  const values = Object.fromEntries(await request.formData());
  const errors: Record<string, string> = {};
  const { id } = params;
  const channelName = typeof values.channelName === 'string' ? values.channelName.trim() : '';
  const yTChannelId = typeof values.yTChannelId === 'string' ? values.yTChannelId.trim() : '';
  const shortLinkUrl = typeof values.shortLinkUrl === 'string' ? values.shortLinkUrl.trim() : '';
  const socials = parseSocials(values.socials);

  if (!channelName) {
    errors['channelName'] = 'Channel name cannot be empty';
  }

  if (!id && !yTChannelId) {
    errors['yTChannelId'] = 'YouTube Channel ID cannot be empty';
  }

  if (socials === null) {
    errors['socials'] = 'Social entries must be valid provider and handler pairs';
  }

  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  try {
    if (id) {
      const response = await put(ComposeUrl(endpoints.CHANNELS_DETAIL, { channelId: id }), {
        channelId: id,
        channelName,
        shortLinkUrl,
        socials: socials ?? [],
      });
      if (getChannelApiError(response)) {
        return channelActionError(response, 'Unable to update channel');
      }
    } else {
      const payload = {
        channelName,
        yTChannelId,
        shortLinkUrl,
        socials: socials ?? [],
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

function parseSocials(value: FormDataEntryValue | undefined): { provider: string; handler: string }[] | null {
  if (typeof value !== 'string' || !value.trim()) {
    return [];
  }

  try {
    const parsed: unknown = JSON.parse(value);
    if (!Array.isArray(parsed)) return null;

    return parsed
      .filter((social): social is { provider: string; handler: string } =>
        typeof social === 'object' && social !== null &&
        'provider' in social && typeof social.provider === 'string' &&
        'handler' in social && typeof social.handler === 'string')
      .map(social => ({ provider: social.provider.toLowerCase().trim(), handler: social.handler.trim() }))
      .filter(social => social.provider && social.handler);
  } catch {
    return null;
  }
}
