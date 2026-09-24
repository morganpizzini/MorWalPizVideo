import { endpoints, get } from '@morwalpizvideo/services';

export default async function loader({ params }: { params: Record<string, string | undefined> }) {
  if (!params.id) return null;
  return get(`${endpoints.SURVEYS_DETAIL.replace('{surveyId}', encodeURIComponent(params.id))}`);
}
