import { insightsTopicsApi } from '@morwalpizvideo/services';
import { LoaderFunctionArgs } from 'react-router';

export default async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;

  if (id) {
    return await insightsTopicsApi.getById(id);
  }

  return null;
}