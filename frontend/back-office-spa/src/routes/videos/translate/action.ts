import { data } from 'react-router';
import { post } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());

  try {
    await post(endpoints.VIDEOS_TRANSLATE, values);
    return data({ success: true }, { status: 200 });
  } catch (error) {
    return data({ success: false, errors: { generics: ['API error found'] } }, { status: 500 });
  }
}
