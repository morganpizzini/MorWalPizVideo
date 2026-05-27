import { ActionFunctionArgs, data } from 'react-router';
import { post, put, endpoints, ComposeUrl } from '@morwalpizvideo/services';

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
    target: values.target as string,
    linkType: parseInt(values.linkType as string),
    queryLinkIds: JSON.parse((values.queryLinkIds as string) || '[]'),
    message: (values.message as string) || '',
  };

  try {
    if (id) {
      await put(ComposeUrl(endpoints.SHORTLINKS_DETAIL, { querylinkId: id }), {
        ...payload,
        shortLinkId: id,
      });
    } else {
      await post(endpoints.SHORTLINKS, payload);
    }
    return data({ success: true }, { status: id ? 200 : 201 });
  } catch (error) {
    return data({ success: false, errors: { generics: ['API error found'] } }, { status: 500 });
  }
}
