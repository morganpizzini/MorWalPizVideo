import { data } from 'react-router';
import { createGallery, updateGallery } from '@morwalpizvideo/services';
import type { SaveGalleryRequest } from '@morwalpizvideo/models';

export default async function action({
  request,
  params,
}: {
  request: Request;
  params: { id?: string };
}) {
  const formData = await request.formData();
  const name = String(formData.get('name') ?? '').trim();
  const slug = String(formData.get('slug') ?? '').trim();
  const description = String(formData.get('description') ?? '').trim();
  const expirationDays = Number(
    formData.get('expirationDays') ?? 7
  ) as SaveGalleryRequest['expirationDays'];
  const errors: Record<string, string> = {};
  if (!name) errors.name = 'Name is required';
  if (!slug) errors.slug = 'Slug is required';
  if (![3, 7, 21].includes(expirationDays)) errors.expirationDays = 'Expiration is invalid';
  if (Object.keys(errors).length) return data({ success: false, errors }, { status: 400 });
  const payload: SaveGalleryRequest = { name, slug, description, expirationDays };
  try {
    const response = params.id
      ? await updateGallery(params.id, payload)
      : await createGallery(payload);
    if (response && 'errors' in response && response.errors) {
      return data(
        { success: false, errors: { generics: response.errors } },
        { status: response.status ?? 500 }
      );
    }
    return data({ success: true }, { status: params.id ? 200 : 201 });
  } catch {
    return data(
      { success: false, errors: { generics: ['Failed to save gallery'] } },
      { status: 500 }
    );
  }
}
