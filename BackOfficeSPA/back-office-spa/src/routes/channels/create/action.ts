import { data } from 'react-router';
import { API_CONFIG } from '@config/api';
import { CreateChannelDTO } from '@/models/channel';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData()) as CreateChannelDTO;

  const errors: Record<string, string | string[]> = {};

  if (!values.channelName || values.channelName.toString().trim().length === 0) {
    errors['channelName'] = 'Channel Name cannot be empty';
  }

  if (!values.yTChannelId || values.yTChannelId.toString().trim().length === 0) {
    errors['yTChannelId'] = 'YouTube Channel ID cannot be empty';
  }

  // Return errors if any
  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  // API request
  return fetch(`${API_CONFIG.BASE_URL}/channels`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(values),
  })
    .then(() => {
      return data({ success: true }, { status: 201 });
    })
    .catch(() => {
      errors['generics'] = ['API error found'];
      return data({ success: false, errors }, { status: 500 });
    });
}
