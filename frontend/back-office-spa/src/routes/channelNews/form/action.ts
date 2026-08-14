import { data, type ActionFunctionArgs } from 'react-router';
import {
  createChannelNews,
  updateChannelNews,
  uploadChannelNewsImages,
} from '@morwalpizvideo/services';
import type { ChannelNewsAdmin } from '@morwalpizvideo/models';

export default async function action({ request, params }: ActionFunctionArgs) {
  const formData = await request.formData();
  const values = Object.fromEntries(formData);
  const files = formData
    .getAll('images')
    .filter((value): value is File => value instanceof File && value.size > 0);
  const title = String(values.title ?? '').trim();
  const status = Number(values.status ?? 0);
  const errors: Record<string, string> = {};

  if (!title) errors.title = 'Title is required';
  if (![0, 1, 2, 3].includes(status)) errors.status = 'Status is invalid';
  if (status === 1 && !String(values.publicationTimeUtc ?? '').trim())
    errors.publicationTimeUtc = 'Publication time is required for scheduled news';
  if (files.length > 10) errors.images = 'A ChannelNews item can contain at most 10 images';
  if (Object.keys(errors).length) return data({ success: false, errors }, { status: 400 });

  const payload = {
    title,
    subtitle: String(values.subtitle ?? '').trim(),
    descriptionHtml: String(values.descriptionHtml ?? ''),
    slug: String(values.slug ?? '').trim(),
    status,
    publicationTimeUtc: String(values.publicationTimeUtc ?? '').trim() || null,
    displayOrder: Number(values.displayOrder ?? 0) || 0,
  };

  const response = (
    params.id ? await updateChannelNews(params.id, payload) : await createChannelNews(payload)
  ) as ChannelNewsAdmin & { errors?: string[]; status?: number };
  if (response?.errors)
    return data(
      { success: false, errors: { generics: response.errors } },
      { status: response.status ?? 500 }
    );
  if (!params.id && files.length > 0) {
    const uploaded = (await uploadChannelNewsImages(response.id, files)) as ChannelNewsAdmin & {
      errors?: string[];
      status?: number;
    };
    if (uploaded?.errors)
      return data(
        { success: false, errors: { images: uploaded.errors } },
        { status: uploaded.status ?? 400 }
      );
    return data({ success: true, entity: uploaded }, { status: 201 });
  }
  return data({ success: true, entity: response }, { status: params.id ? 200 : 201 });
}
