import { ActionFunctionArgs, data } from 'react-router';
import { put } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

export default async function action({ request, params }: ActionFunctionArgs) {
  const values = Object.fromEntries(await request.formData());

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
