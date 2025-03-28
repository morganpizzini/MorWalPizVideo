import { data } from 'react-router';
import { API_CONFIG } from '@config/api';
import { UpdateChannelDTO } from '@/models/channel';

export default async function action({
  request,
  params,
}: {
  request: Request;
  params: { id: string };
}) {
  const values = Object.fromEntries(await request.formData()) as UpdateChannelDTO;
  const errors: Record<string, string | string[]> = {};

  if (!values.channelName || values.channelName.trim().length === 0) {
    errors['channelName'] = 'Channel Name cannot be empty';
  }

  // Return errors if any
  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  // API request
  return fetch(`${API_CONFIG.BASE_URL}/channels/${params.id}`, {
    method: 'PUT',
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
