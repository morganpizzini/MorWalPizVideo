import { data, type ActionFunctionArgs } from 'react-router';
import { createFaqAnswer, updateFaqAnswer } from '@morwalpizvideo/services';

export default async function action({ request, params }: ActionFunctionArgs) {
  const values = Object.fromEntries(await request.formData());
  const content = String(values.content ?? '').trim();
  const status = Number(values.status ?? 0);
  if (!content) return data({ success: false, errors: { content: 'Answer content is required.' } }, { status: 400 });
  if (![0, 1, 2].includes(status)) return data({ success: false, errors: { status: 'Status is invalid.' } }, { status: 400 });
  try {
    const answerId = String(values.answerId ?? '');
    const payload = { content, status };
    const entity = answerId ? await updateFaqAnswer(answerId, payload) : await createFaqAnswer(params.id ?? '', payload);
    return data({ success: true, entity });
  } catch {
    return data({ success: false, errors: { generics: 'Unable to save answer.' } }, { status: 500 });
  }
}
