import { data } from 'react-router';
import { Delete } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

export default async function action({ params }: { params: { title: string } }) {
  const title = decodeURIComponent(params.title);
  
  try {
    const response = await Delete(ComposeUrl(endpoints.CALENDAREVENTS_DETAIL, { title: encodeURIComponent(title) }));
    
    if (response?.errors) {
      return data({ success: false, errors: response.errors }, { status: 400 });
    }
    
    return data({ success: true }, { status: 200 });
  } catch (error) {
    return data({ success: false, errors: { generics: ['API error found'] } }, { status: 500 });
  }
}
