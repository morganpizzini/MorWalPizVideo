import { data } from 'react-router';
import { post, endpoints } from '@morwalpizvideo/services';

export default async function action({ request }: { request: Request }) {
    const values = Object.fromEntries(await request.formData());
    const categories = JSON.parse(values.categories);
  try {
      await post(endpoints.VIDEOS_IMPORT, { ...values, categories });
    return data({ success: true }, { status: 201 });
  } catch (error) {
    return data({ success: false, errors: { generics: ['API error found'] } }, { status: 500 });
  }
}
