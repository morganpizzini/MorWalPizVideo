import { data } from 'react-router';
import { post } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());
  const errors: Record<string, string | string[]> = {};

  if (!values.title || (values.title as string).trim().length === 0) {
    errors['title'] = 'Title cannot be empty';
  }

  if (!values.longUrl || (values.longUrl as string).trim().length === 0) {
    errors['longUrl'] = 'URL cannot be empty';
  }

  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  try {
    await post(endpoints.SHORTLINKS, values);
    return data({ success: true }, { status: 201 });
  } catch (error) {
    errors['generics'] = ['API error found'];
    return data({ success: false, errors }, { status: 500 });
  }
}
