import { data } from 'react-router';
import { createPage, updatePage } from '@morwalpizvideo/services';
import type { CreatePageDTO, PageStatus, UpdatePageDTO } from '@morwalpizvideo/models';
import { getPagesApiError } from '../response';

export default async function action({ request, params }: { request: Request; params: { id?: string } }) {
  const values = Object.fromEntries(await request.formData());
  const title = String(values.title ?? '').trim();
  const url = String(values.url ?? '').trim();
  const status = Number(values.status ?? 0) as PageStatus;
  const errors: Record<string, string> = {};
  if (!title) errors.title = 'Title is required';
  if (!url) errors.url = 'URL is required';
  if (![0, 1].includes(status)) errors.status = 'Status is invalid';
  if (Object.keys(errors).length) return data({ success: false, errors }, { status: 400 });
  const payload: CreatePageDTO = { title, url, status, content: String(values.content ?? ''), thumbnailUrl: String(values.thumbnailUrl ?? '').trim(), videoId: String(values.videoId ?? '').trim() };
  try {
    const response = params.id ? await updatePage(params.id, payload as UpdatePageDTO) : await createPage(payload);
    const error = getPagesApiError(response);
    if (error) return data({ success: false, errors: { generics: error.errors } }, { status: error.status ?? 500 });
    return data({ success: true, entity: response }, { status: params.id ? 200 : 201 });
  } catch {
    return data({ success: false, errors: { generics: ['Failed to save page'] } }, { status: 500 });
  }
}