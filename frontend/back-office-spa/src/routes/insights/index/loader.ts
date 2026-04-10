import { insightsTopicsApi } from '@morwalpizvideo/services';

export default async function loader() {
  return await insightsTopicsApi.getAll();
}