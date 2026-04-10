import { ActionFunctionArgs, data } from 'react-router';
import { put, endpoints, ComposeUrl } from '@morwalpizvideo/services';
import { UpdateChannelDTO } from '@/models';

export default async function action({ request, params }: ActionFunctionArgs) {
  const values = Object.fromEntries(await request.formData()) as UpdateChannelDTO;
  const errors: Record<string, string | string[]> = {};

  // Validate fields
  if (!values.title || values.title.trim().length === 0) {
    errors['title'] = 'Title cannot be empty';
  }

  if (!values.url || values.url.trim().length === 0) {
    errors['url'] = 'URL cannot be empty';
  }

  // Check for errors
  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  try {
    await put(ComposeUrl(endpoints.CHANNELS_DETAIL, { channelId: params.id! }), values);
    return data({ success: true }, { status: 200 });
  } catch (error) {
    errors['generics'] = ['API error found'];
    return data({ success: false, errors }, { status: 500 });
  }
}
