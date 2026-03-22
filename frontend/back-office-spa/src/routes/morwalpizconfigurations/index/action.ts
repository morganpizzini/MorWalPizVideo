import { data } from 'react-router';
import { Delete } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

export default async function action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());

  try {
    await Delete(ComposeUrl(endpoints.CONFIGURATIONS_DETAIL, { configurationId: values.id as string }));
    return data({ success: true }, { status: 200 });
  } catch (error) {
    return data({ success: false }, { status: 500 });
  }
}
