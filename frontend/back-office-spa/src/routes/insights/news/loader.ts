import { insightsNewsApi } from '@morwalpizvideo/services';
import { LoaderFunctionArgs } from 'react-router';

export default async function loader({ params }: LoaderFunctionArgs) {
  const { newsId } = params;

  if (!newsId) {
    throw new Response('News ID is required', { status: 400 });
  }

  return await insightsNewsApi.getById(newsId);
}