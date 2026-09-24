import { redirect } from 'react-router';
import { ComposeUrl, endpoints, post, put } from '@morwalpizvideo/services';

export default async function action({
  request,
  params,
}: {
  request: Request;
  params: Record<string, string | undefined>;
}) {
  const formData = await request.formData();
  const payload = {
    title: String(formData.get('title') ?? ''),
    description: String(formData.get('description') ?? ''),
    url: String(formData.get('url') ?? ''),
    fromUtc: new Date(String(formData.get('fromUtc'))).toISOString(),
    toUtc: new Date(String(formData.get('toUtc'))).toISOString(),
    formIds: JSON.parse(String(formData.get('formIds') ?? '[]')),
    lifecycle: String(formData.get('lifecycle') ?? 'Draft'),
  };
  try {
    if (params.id)
      await put(
        ComposeUrl(endpoints.SURVEYS_DETAIL, { surveyId: encodeURIComponent(params.id) }),
        payload
      );
    else await post(endpoints.SURVEYS, payload);
    return redirect('/surveys');
  } catch (error) {
    return { success: false, errors: { generics: [(error as Error).message] } };
  }
}
