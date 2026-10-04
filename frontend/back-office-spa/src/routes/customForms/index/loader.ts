import { get, endpoints } from '@morwalpizvideo/services';

export default async function loader() {
  try {
    const response = (await get(endpoints.CUSTOMFORMS)) as unknown;
    if (
      response &&
      typeof response === 'object' &&
      Array.isArray((response as { errors?: unknown }).errors)
    ) {
      throw new Error('Unable to load custom forms. Please try again.');
    }
    return response;
  } catch {
    throw new Error('Unable to load custom forms. Please try again.');
  }
}
