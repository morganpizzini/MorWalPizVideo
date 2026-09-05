import { data, type ActionFunctionArgs } from 'react-router';
import { createFaq, updateFaq } from '@morwalpizvideo/services';

export default async function action({ request, params }: ActionFunctionArgs) {
  const values = Object.fromEntries(await request.formData());
  const question = String(values.question ?? '').trim();
  const categoryId = String(values.categoryId ?? '').trim();
  const status = Number(values.status ?? 0);
  const errors: Record<string, string> = {};
  if (!question) errors.question = 'Question is required.';
  if (!categoryId) errors.categoryId = 'Category is required.';
  if (![0, 1, 2].includes(status)) errors.status = 'Status is invalid.';
  if (Object.keys(errors).length) return data({ success: false, errors }, { status: 400 });

  const payload = { question, categoryId, status, sourceMetadata: { sourceType: 'Manual', campaignIds: [], provider: '' } };
  try {
    const entity = params.id ? await updateFaq(params.id, payload) : await createFaq(payload);
    return data({ success: true, entity }, { status: params.id ? 200 : 201 });
  } catch {
    return data({ success: false, errors: { generics: 'Unable to save FAQ.' } }, { status: 500 });
  }
}
