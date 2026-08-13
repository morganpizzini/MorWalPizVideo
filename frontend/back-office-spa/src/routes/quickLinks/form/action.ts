import { data, type ActionFunctionArgs } from 'react-router';
import {
  createQuickLinks,
  updateQuickLinks,
} from '@morwalpizvideo/services';
import type { CreateQuickLinksDTO, QuickLink, UpdateQuickLinksDTO } from '@morwalpizvideo/models';
import { getQuickLinksApiError, quickLinksActionError } from '../response';

export default async function action({ request, params }: ActionFunctionArgs) {
  const formData = await request.formData();
  const title = String(formData.get('title') ?? '').trim();
  const url = String(formData.get('url') ?? '').trim();
  const subtitle = String(formData.get('subtitle') ?? '').trim();
  const rawLinks = String(formData.get('links') ?? '[]');
  const errors: Record<string, string> = {};

  if (!title) errors.title = 'Title is required';
  if (!url) errors.url = 'URL is required';

  let links: QuickLink[] = [];
  try {
    links = JSON.parse(rawLinks) as QuickLink[];
    if (!Array.isArray(links)) throw new Error('Links must be an array');
  } catch {
    errors.links = 'Links must be valid JSON';
  }

  if (Object.keys(errors).length > 0) return data({ success: false, errors }, { status: 400 });

  const payload = { title, subtitle: subtitle || undefined, url, links };
  try {
    let response: unknown;
    if (params.id) {
      response = await updateQuickLinks(params.id, payload as UpdateQuickLinksDTO);
    } else {
      response = await createQuickLinks(payload as CreateQuickLinksDTO);
    }
    if (getQuickLinksApiError(response)) {
      return quickLinksActionError(response, 'Failed to save QuickLinks');
    }
    return data({ success: true }, { status: params.id ? 200 : 201 });
  } catch {
    return data({ success: false, errors: { generics: ['Failed to save QuickLinks'] } }, { status: 500 });
  }
}
