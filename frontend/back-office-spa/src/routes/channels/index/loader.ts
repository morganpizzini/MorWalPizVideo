import { get, endpoints } from '@morwalpizvideo/services';

export default async function loader() {
  return get(endpoints.CHANNELS);
}
