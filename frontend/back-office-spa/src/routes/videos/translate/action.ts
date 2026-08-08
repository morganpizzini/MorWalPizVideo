import { data } from 'react-router';
import { post, endpoints } from '@morwalpizvideo/services';

export default async function action({ request }: { request: Request }) {
  const videoIds = (await request.formData())
    .getAll('videoIds')
    .map(value => String(value).trim())
    .filter(Boolean);

  try {
    await post(endpoints.VIDEOS_TRANSLATE, videoIds);
    return data({ success: true }, { status: 200 });
  } catch (error) {
    return data({ success: false, errors: { generics: ['API error found'] } }, { status: 500 });
  }
}
