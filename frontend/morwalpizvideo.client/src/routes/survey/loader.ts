import type { LoaderFunctionArgs } from 'react-router';
import { getPublicSurveyByUrl } from '../../services/surveys';

export default async function surveyLoader({ params }: LoaderFunctionArgs) {
  if (!params.surveyUrl) throw new Error('Survey URL is required');
  return { survey: await getPublicSurveyByUrl(params.surveyUrl) };
}
