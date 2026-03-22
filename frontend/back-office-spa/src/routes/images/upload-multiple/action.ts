import { data } from 'react-router';
import { postFormData } from '@services/apiService';
import endpoints from '@services/endpoints';

export default async function action({ request }: { request: Request }) {
  const formData = await request.formData();

  try {
    await postFormData(endpoints.IMAGE_UPLOAD_MULTIPLE, formData);
    return data({ success: true }, { status: 201 });
  } catch (error) {
    return data({ success: false, errors: { generics: ['API error found'] } }, { status: 500 });
  }
}
