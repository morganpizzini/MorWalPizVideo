import { insightsTopicsApi } from '@morwalpizvideo/services';
import { LoaderFunctionArgs } from 'react-router';

export default async function action({ request }: LoaderFunctionArgs) {
  const formData = await request.formData();
  const topicId = formData.get('topicId') as string;

  if (!topicId) {
    return {
      success: false,
      errors: {
        generics: ['Topic ID is required'],
      },
    };
  }

  try {
    await insightsTopicsApi.scanNews(topicId);
    return { success: true };
  } catch (error: any) {
    return {
      success: false,
      errors: {
        generics: [error.message || 'Failed to scan for news'],
      },
    };
  }
}