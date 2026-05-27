import { ActionFunctionArgs, data } from 'react-router';
import { post, put, endpoints, ComposeUrl } from '@morwalpizvideo/services';

export default async function action({ request, params }: ActionFunctionArgs) {
  const values = Object.fromEntries(await request.formData());
  const errors: Record<string, string> = {};
  const { id } = params;

  if (!values.channelName || (values.channelName as string).trim().length === 0) {
    errors['channelName'] = 'Channel name cannot be empty';
  }

  if (!id && (!values.yTChannelId || (values.yTChannelId as string).trim().length === 0)) {
    errors['yTChannelId'] = 'YouTube Channel ID cannot be empty';
  }

  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  try {
    if (id) {
      await put(ComposeUrl(endpoints.CHANNELS_DETAIL, { channelId: id }), {
        channelId: id,
        channelName: values.channelName,
      });
    } else {
      await post(endpoints.CHANNELS, {
        channelName: values.channelName,
        yTChannelId: values.yTChannelId,
      });
    }
    return data({ success: true }, { status: id ? 200 : 201 });
  } catch (error) {
    return data({ success: false, errors: { generics: ['API error found'] } }, { status: 500 });
  }
}
