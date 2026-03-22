import { ActionFunctionArgs, data } from 'react-router';
import { put } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

export default async function action({ request, params }: ActionFunctionArgs) {
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
    await put(ComposeUrl(endpoints.SHORTLINKS_DETAIL, { querylinkId: params.id! }), values);
    return data({ success: true }, { status: 200 });
  } catch (error) {
    errors['generics'] = ['API error found'];
    return data({ success: false, errors }, { status: 500 });
  }
}
