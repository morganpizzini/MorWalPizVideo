import { insightsTopicsApi } from '@morwalpizvideo/services';
import { LoaderFunctionArgs } from 'react-router';

export default async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  
  if (!id) {
    throw new Response('Topic ID is required', { status: 400 });
  }

  const [topic, newsItems, contentPlans] = await Promise.all([
    insightsTopicsApi.getById(id),
    insightsTopicsApi.getNews(id),
    insightsTopicsApi.getContentPlans(id),
  ]);

  return { topic, newsItems, contentPlans };
}