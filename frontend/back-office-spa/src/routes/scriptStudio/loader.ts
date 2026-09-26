import { getScriptStudio } from '@morwalpizvideo/services';

export default async function Loader() {
  return { document: await getScriptStudio() };
}
