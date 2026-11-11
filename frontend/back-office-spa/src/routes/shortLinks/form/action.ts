import { ActionFunctionArgs, data } from 'react-router';
import { post, put, endpoints, ComposeUrl } from '@morwalpizvideo/services';

interface ApiErrorResponse {
  errors?: unknown;
  status?: unknown;
}

const isApiErrorResponse = (response: unknown): response is ApiErrorResponse =>
  typeof response === 'object' &&
  response !== null &&
  ('errors' in response ||
    ('status' in response && typeof response.status === 'number' && response.status >= 400));

const getApiErrorMessages = (errors: unknown): string[] =>
  Array.isArray(errors) ? errors.map(error => String(error)) : [];

export default async function action({ request, params }: ActionFunctionArgs) {
  const values = Object.fromEntries(await request.formData());
  const errors: Record<string, string> = {};
  const { id } = params;

  if (!values.target || (values.target as string).trim().length === 0) {
    errors['target'] = 'Target cannot be empty';
  }

  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  const payload = {
    ...(id ? { code: String(values.code ?? '').trim() } : {}),
    target: values.target as string,
    linkType: parseInt(values.linkType as string),
    queryLinkIds: JSON.parse((values.queryLinkIds as string) || '[]'),
  };

  try {
    let response: unknown;
    if (id) {
      response = await put(ComposeUrl(endpoints.SHORTLINKS_DETAIL, { querylinkId: id }), {
        ...payload,
        shortLinkId: id,
      });
    } else {
      response = await post(endpoints.SHORTLINKS, payload);
    }

    if (isApiErrorResponse(response)) {
      const generics = getApiErrorMessages(response.errors);
      return data(
        {
          success: false,
          errors: {
            generics: generics.length > 0 ? generics : ['Short link could not be saved.'],
          },
        },
        {
          status:
            typeof response.status === 'number' && response.status >= 400
              ? response.status
              : 500,
        }
      );
    }

    return data({ success: true }, { status: id ? 200 : 201 });
  } catch (error) {
    return data({ success: false, errors: { generics: ['API error found'] } }, { status: 500 });
  }
}
