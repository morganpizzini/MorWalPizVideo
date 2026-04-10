import { data } from 'react-router';
import { post, endpoints } from '@morwalpizvideo/services';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());
  const errors: Record<string, string | string[]> = {};

  if (!values.title || (values.title as string).trim().length === 0) {
    errors['title'] = 'Title cannot be empty';
  }

  if (!values.link || (values.link as string).trim().length === 0) {
    errors['link'] = 'Link cannot be empty';
  }

  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  try {
    await post(endpoints.QUERYLINKS, values);
    return data({ success: true }, { status: 201 });
  } catch (error) {
    errors['generics'] = ['API error found'];
    return data({ success: false, errors }, { status: 500 });
  }
}
