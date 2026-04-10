import { get, endpoints } from '@morwalpizvideo/services';

export default async function loader() {
  try {
    const response = await get(endpoints.CUSTOMFORMS);
    return response;
  } catch (error) {
    return [];
  }
}
