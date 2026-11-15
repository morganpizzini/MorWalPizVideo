import { data, type ActionFunctionArgs } from 'react-router';
import type { FeedbackStatus } from '@morwalpizvideo/models';
import { ApiResponseError, deleteFeedback, updateFeedbackStatus } from '@morwalpizvideo/services';

export default async function action({ request }: ActionFunctionArgs) {
  const values = Object.fromEntries(await request.formData());
  const id = typeof values.id === 'string' ? values.id : '';
  const intent = values.intent;

  if (!id || (intent !== 'save' && intent !== 'delete')) {
    return data({ error: 'Invalid feedback action.' }, { status: 400 });
  }

  try {
    if (intent === 'delete') {
      await deleteFeedback(id);
      return data({ deleted: true });
    }

    const status = values.status;
    const expectedRevision = Number(values.expectedRevision);
    if (!['New', 'Reviewing', 'Resolved', 'Dismissed'].includes(String(status)) || !Number.isInteger(expectedRevision)) {
      return data({ error: 'Invalid feedback status.' }, { status: 400 });
    }

    const item = await updateFeedbackStatus(id, {
      status: status as FeedbackStatus,
      expectedRevision,
    });
    return data({ item });
  } catch (error) {
    const conflict = error instanceof ApiResponseError && error.status === 409;
    return data({
      conflict,
      error: conflict
        ? 'This feedback was changed by another administrator. Reload it before saving again.'
        : intent === 'delete'
          ? 'The feedback could not be deleted. It may have already been removed.'
          : 'The feedback could not be updated. Check your connection and try again.',
    }, { status: conflict ? 409 : 500 });
  }
}
