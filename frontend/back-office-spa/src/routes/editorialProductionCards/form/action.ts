import { data } from 'react-router';
import {
  createEditorialProductionCard,
  updateEditorialProductionCard,
} from '@morwalpizvideo/services';
import type {
  EditorialProductionCardRequest,
  EditorialProductionCardStatus,
} from '@morwalpizvideo/models';

export default async function action({
  request,
  params,
}: {
  request: Request;
  params: { id?: string };
}) {
  const values = Object.fromEntries(await request.formData());
  const title = String(values.title ?? '').trim();
  const errors: Record<string, string> = {};
  if (!title) errors.title = 'Title is required';
  if (Object.keys(errors).length) return data({ success: false, errors }, { status: 400 });
  const payload: EditorialProductionCardRequest = {
    title,
    editorialAngle: String(values.editorialAngle ?? '').trim(),
    sourceUrls: String(values.sourceUrls ?? '')
      .split('\n')
      .map(value => value.trim())
      .filter(Boolean),
    brief: String(values.brief ?? '').trim(),
    script: String(values.script ?? '').trim(),
    status: Number(values.status ?? 0) as EditorialProductionCardStatus,
    ownerUserId: String(values.ownerUserId ?? '').trim() || undefined,
    dueDateUtc: String(values.dueDateUtc ?? '').trim() || undefined,
    topicId: String(values.topicId ?? '').trim() || undefined,
    newsItemId: String(values.newsItemId ?? '').trim() || undefined,
    contentPlanId: String(values.contentPlanId ?? '').trim() || undefined,
    youtubeVideoId: String(values.youtubeVideoId ?? '').trim() || undefined,
    backOfficeContentId: String(values.backOfficeContentId ?? '').trim() || undefined,
    ...(params.id ? { revision: Number(values.revision ?? 0) } : {}),
  };
  try {
    const response = params.id
      ? await updateEditorialProductionCard(params.id, payload)
      : await createEditorialProductionCard(payload);
    return data({ success: true, entity: response }, { status: params.id ? 200 : 201 });
  } catch (error) {
    return data(
      {
        success: false,
        errors: { generics: [error instanceof Error ? error.message : 'Unable to save the card'] },
      },
      { status: 409 }
    );
  }
}
