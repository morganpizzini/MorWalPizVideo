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
  const isSHIT = values.isSHIT === 'true';
  const socials = parseSocials(values.socials);
  const socialPublishing = parseSocialPublishing(values.socialPublishing);

  if (!channelName) {
    errors['channelName'] = 'Channel name cannot be empty';
  }

  if (!id && !yTChannelId) {
    errors['yTChannelId'] = 'YouTube Channel ID cannot be empty';
  }

  if (socials === null) {
    errors['socials'] = 'Social entries must be valid provider and handler pairs';
  }

  if (socialPublishing === null) {
    errors['socialPublishing'] = 'Social publishing configuration is invalid';
  }

  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  try {
    let cacheInvalidation: unknown;
    if (id) {
      const payload = {
        channelId: id,
        channelName,
        shortLinkUrl,
        socials: socials ?? [],
        socialPublishing,
        ...(typeof values.isSHIT === 'string' ? { isSHIT } : {}),
      };
      const response = await put(ComposeUrl(endpoints.CHANNELS_DETAIL, { channelId: id }), payload);
      if (getChannelApiError(response)) {
        return channelActionError(response, 'Unable to update channel');
      }
      cacheInvalidation = response?.cacheInvalidation;
    } else {
      const payload = {
        channelName,
        yTChannelId,
        shortLinkUrl,
        isSHIT,
        socials: socials ?? [],
        socialPublishing,
      };
      const response = await post(endpoints.CHANNELS, payload);
      if (getChannelApiError(response)) {
        return channelActionError(response, 'Unable to create channel');
      }
      cacheInvalidation = response?.cacheInvalidation;
    }
    return data({ success: true, cacheInvalidation }, { status: id ? 200 : 201 });
  } catch (error) {
    return channelActionError(error, id ? 'Unable to update channel' : 'Unable to create channel');
  }
}

type PublishingProviderPayload = {
  destinationId?: string;
  credential?: string;
  clearCredential: boolean;
};

function parseSocialPublishing(value: FormDataEntryValue | undefined): Record<string, PublishingProviderPayload> | null {
  const emptyProvider = (): PublishingProviderPayload => ({ clearCredential: false });
  if (typeof value !== 'string' || !value.trim()) {
    return { telegram: emptyProvider(), discord: emptyProvider(), facebook: emptyProvider() };
  }

  try {
    const parsed: unknown = JSON.parse(value);
    if (typeof parsed !== 'object' || parsed === null) return null;

    const result: Record<string, PublishingProviderPayload> = {};
    for (const provider of ['telegram', 'discord', 'facebook']) {
      const candidate = (parsed as Record<string, unknown>)[provider];
      if (typeof candidate !== 'object' || candidate === null) return null;
      const values = candidate as Record<string, unknown>;
      if (values.destinationId !== undefined && typeof values.destinationId !== 'string') return null;
      if (values.credential !== undefined && typeof values.credential !== 'string') return null;
      if (values.clearCredential !== undefined && typeof values.clearCredential !== 'boolean') return null;

      result[provider] = {
        destinationId: typeof values.destinationId === 'string' ? values.destinationId.trim() : undefined,
        credential: typeof values.credential === 'string' && values.credential.trim()
          ? values.credential.trim()
          : undefined,
        clearCredential: values.clearCredential === true,
      };
    }
    return result;
  } catch {
    return null;
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
