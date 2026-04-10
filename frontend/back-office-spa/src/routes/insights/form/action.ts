import { insightsTopicsApi } from '@morwalpizvideo/services';
import { LoaderFunctionArgs } from 'react-router';
import { CreateInsightTopicRequest, UpdateInsightTopicRequest } from '@morwalpizvideo/models';

export default async function action({ request, params }: LoaderFunctionArgs) {
  const formData = await request.formData();
  const { id } = params;

  const title = formData.get('title') as string;
  const description = formData.get('description') as string;
  const seedArgumentsJson = formData.get('seedArguments') as string;
  const preferredSourcesJson = formData.get('preferredSources') as string;

  const seedArguments = seedArgumentsJson ? JSON.parse(seedArgumentsJson) : [];
  const preferredSources = preferredSourcesJson ? JSON.parse(preferredSourcesJson) : [];

  if (!title || !description) {
    return {
      success: false,
      errors: {
        generics: ['Title and description are required'],
      },
    };
  }

  try {
    if (id) {
      // Update existing topic
      const updateRequest: UpdateInsightTopicRequest = {
        title,
        description,
        seedArguments,
        preferredSources,
      };
      await insightsTopicsApi.update(id, updateRequest);
    } else {
      // Create new topic
      const createRequest: CreateInsightTopicRequest = {
        title,
        description,
        seedArguments,
        preferredSources,
      };
      await insightsTopicsApi.create(createRequest);
    }
    return { success: true };
  } catch (error: any) {
    return {
      success: false,
      errors: {
        generics: [error.message || `Failed to ${id ? 'update' : 'create'} topic`],
      },
    };
  }
}