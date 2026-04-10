import { insightsTopicsApi } from '@morwalpizvideo/services';

export default async function action({ request }: { request: Request }) {
  const formData = await request.formData();
  const id = formData.get('id') as string;

  if (!id) {
    return {
      success: false,
      errors: {
        generics: ['Topic ID is required'],
      },
    };
  }

  try {
    await insightsTopicsApi.delete(id);
    return { success: true };
  } catch (error: any) {
    return {
      success: false,
      errors: {
        generics: [error.message || 'Failed to delete topic'],
      },
    };
  }
}