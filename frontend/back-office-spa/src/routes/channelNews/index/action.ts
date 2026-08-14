import { data } from 'react-router';
import { deleteChannelNews } from '@morwalpizvideo/services';

export default async function action({ request }: { request: Request }) {
  const id = String((await request.formData()).get('id') ?? '').trim();
  if (!id) return data({ errors: ['ChannelNews ID is required'] }, { status: 400 });

  const response = await deleteChannelNews(id);
  if (response?.errors)
    return data({ errors: response.errors }, { status: response.status ?? 500 });
  return { success: true };
}
