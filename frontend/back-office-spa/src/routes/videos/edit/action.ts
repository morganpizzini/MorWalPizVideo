import { ActionFunctionArgs, data } from 'react-router';
import { post, put, endpoints, ComposeUrl } from '@morwalpizvideo/services';
import type { VideoReferenceAddRequest } from '@morwalpizvideo/models';

const formatError = (error: unknown): string => {
  if (typeof error === 'string') {
    return error;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return JSON.stringify(error) ?? 'API error found';
};

const normalizeErrors = (errors: unknown): string[] =>
  (Array.isArray(errors) ? errors : [errors]).map(formatError);

export default async function action({ request, params }: ActionFunctionArgs) {
  const formData = await request.formData();
  if (formData.get('_intent') === 'addVideoReference') {
    return addVideoReference(formData, params.id!);
  }

  const values = Object.fromEntries(formData) as Record<string, unknown>;
  const parseErrors: string[] = [];

  for (const field of ['categories']) {
    const value = values[field];
    if (typeof value !== 'string') {
      continue;
    }

    try {
      values[field] = JSON.parse(value);
    } catch {
      parseErrors.push(`${field} must contain valid JSON`);
    }
  }

  async function addVideoReference(formData: FormData, videoId: string) {
    const youtubeId = String(formData.get('youtubeId') ?? '').trim();
    const categoriesValue = formData.get('categories');
    let categories: unknown;

    if (typeof categoriesValue !== 'string') {
      return data(
        { success: false, errors: { generics: ['categories must contain valid JSON'] } },
        { status: 400 }
      );
    }

    try {
      categories = JSON.parse(categoriesValue);
    } catch {
      return data(
        { success: false, errors: { generics: ['categories must contain valid JSON'] } },
        { status: 400 }
      );
    }

    if (!Array.isArray(categories) || categories.some(category => typeof category !== 'string')) {
      return data(
        { success: false, errors: { generics: ['categories must contain a JSON array'] } },
        { status: 400 }
      );
    }

    try {
      const payload: VideoReferenceAddRequest = { youtubeId, categories };
      const response = await post(
        ComposeUrl(endpoints.VIDEOS_VIDEO_REFS, { videoId }),
        payload
      );

      if (response?.errors) {
        return data(
          { success: false, errors: { generics: normalizeErrors(response.errors) } },
          { status: response.status ?? 500 }
        );
      }

      return data({ success: true, videoRef: response }, { status: 200 });
    } catch (error: unknown) {
      return data(
        { success: false, errors: { generics: [formatError(error)] } },
        { status: 500 }
      );
    }
  }

  if (parseErrors.length > 0) {
    return data({ success: false, errors: { generics: parseErrors } }, { status: 400 });
  }

  try {
    const response = await put(ComposeUrl(endpoints.VIDEOS_DETAIL, { videoId: params.id! }), values);

    if (response?.errors) {
      return data(
        { success: false, errors: { generics: normalizeErrors(response.errors) } },
        { status: response.status ?? 500 }
      );
    }

    return data({ success: true }, { status: 200 });
  } catch (error: unknown) {
    return data(
      { success: false, errors: { generics: [formatError(error)] } },
      { status: 500 }
    );
  }
}
