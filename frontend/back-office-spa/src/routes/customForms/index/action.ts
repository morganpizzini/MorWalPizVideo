import { data } from 'react-router';
import { Delete } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

export default async function action({ request }: { request:  Request }) {
  const formData = await request.formData();
  const id = formData.get('id') as string;
  
  try {
    const response = await Delete(ComposeUrl(endpoints.CUSTOMFORMS_DETAIL, { customFormId: encodeURIComponent(id) }));
    
    if (response?.errors) {
      return data({ success: false, errors: response.errors }, { status: 400 });
    }
    
    return data({ success: true }, { status: 200 });
  } catch (error) {
    return data({ success: false, errors: { generics: ['API error found'] } }, { status: 500 });
  }
}
