import { ActionFunctionArgs, data } from 'react-router';
import { put, endpoints, ComposeUrl } from '@morwalpizvideo/services';

export default async function action({ request, params }: ActionFunctionArgs) {
  const formData = await request.formData();
  const values = Object.fromEntries(formData);
  // Parse categories from JSON string to array
  if (values.categories && typeof values.categories === 'string') {
    try {
      values.categories = JSON.parse(values.categories as string);
    } catch (e) {
      // If parsing fails, keep as is
    }
  }

  try {
    const response = await put(ComposeUrl(endpoints.VIDEOS_DETAIL, { videoId: params.id! }), values);

    if (response?.errors) {
      return data({ success: false, errors: response.errors }, { status: 400 });
    }

    return data({ success: true }, { status: 200 });
  } catch (error) {
    return data({ success: false, errors: { generics: ['API error found'] } }, { status: 500 });
  }
}
