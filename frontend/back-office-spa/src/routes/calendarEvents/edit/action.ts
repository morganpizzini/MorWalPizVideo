import { data } from 'react-router';
import { put, endpoints } from '@morwalpizvideo/services';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());

  try {
    const response = await put(endpoints.CALENDAREVENTS, values);
    
    if (response?.errors) {
      return data({ success: false, errors: response.errors }, { status: 400 });
    }
    
    return data({ success: true }, { status: 200 });
  } catch (error) {
    return data({ success: false, errors: { generics: ['API error found'] } }, { status: 500 });
  }
}
