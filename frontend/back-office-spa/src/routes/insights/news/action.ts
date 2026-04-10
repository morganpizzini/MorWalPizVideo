import { insightsNewsApi } from '@morwalpizvideo/services';
import { LoaderFunctionArgs } from 'react-router';
import { ReviewNewsItemRequest } from '@morwalpizvideo/models';

export default async function action({ request, params }: LoaderFunctionArgs) {
  const formData = await request.formData();
  const { newsId } = params;

  if (!newsId) {
    return {
      success: false,
      errors: {
        generics: ['News ID is required'],
      },
    };
  }

  const starRating = parseInt(formData.get('starRating') as string);
  const status = parseInt(formData.get('status') as string);

  const reviewRequest: ReviewNewsItemRequest = {
    starRating,
    status,
  };

  try {
    await insightsNewsApi.review(newsId, reviewRequest);
    return { success: true };
  } catch (error: any) {
    return {
      success: false,
      errors: {
        generics: [error.message || 'Failed to review news item'],
      },
    };
  }
}