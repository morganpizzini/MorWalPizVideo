import { data } from 'react-router';
import { ComposeUrl, Delete, endpoints } from '@morwalpizvideo/services';

export default async function action({ request }: { request: Request }) {
  const formData = await request.formData();
  const id = String(formData.get('id') ?? '');
  try {
    await Delete(ComposeUrl(endpoints.SURVEYS_DETAIL, { surveyId: encodeURIComponent(id) }));
    return data({ success: true });
  } catch (error) {
    return data(
      { success: false, errors: { generics: [(error as Error).message] } },
      { status: 400 }
    );
  }
}
