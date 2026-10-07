import { getPage } from '@morwalpizvideo/services';
import { get, endpoints } from '@morwalpizvideo/services';
import type { CustomForm, PageAdmin } from '@morwalpizvideo/models';
import { throwIfPagesApiError } from '../response';

export default async function loader({
  params,
}: {
  params: { id?: string };
}): Promise<{ page: PageAdmin | null; customForms: CustomForm[] }> {
  const customFormsPromise = get(endpoints.CUSTOMFORMS);
  if (!params.id) {
    return { page: null, customForms: (await customFormsPromise) as CustomForm[] };
  }

  const [response, customForms] = await Promise.all([getPage(params.id), customFormsPromise]);
  throwIfPagesApiError(response, 'Unable to load page');
  return { page: response as PageAdmin, customForms: customForms as CustomForm[] };
}
