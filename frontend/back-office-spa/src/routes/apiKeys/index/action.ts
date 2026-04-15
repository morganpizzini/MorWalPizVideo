import { data } from 'react-router';
import { Delete, post, endpoints, ComposeUrl } from '@morwalpizvideo/services';

export default async function action({ request }: { request: Request }) {
  const formData = await request.formData();
  const intent = formData.get('intent') as string;
  const id = formData.get('id') as string;

  try {
    if (intent === 'delete') {
      const response = await Delete(
        ComposeUrl(endpoints.APIKEYS_DETAIL, { id: encodeURIComponent(id) })
      );

      if (response?.errors) {
        return data({ success: false, errors: response.errors }, { status: 400 });
      }

      return data({ success: true }, { status: 200 });
    }

    if (intent === 'toggle') {
      const response = await post(
        ComposeUrl(endpoints.APIKEYS_TOGGLE, { id: encodeURIComponent(id) })
      );

      if (response?.errors) {
        return data({ success: false, errors: response.errors }, { status: 400 });
      }

      return data({ success: true, toggleResult: response }, { status: 200 });
    }

    return data({ success: false, errors: { generics: ['Invalid intent'] } }, { status: 400 });
  } catch (error) {
    return data({ success: false, errors: { generics: ['API error found'] } }, { status: 500 });
  }
}