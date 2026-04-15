import { ActionFunctionArgs, redirect } from 'react-router';
import { post, put, endpoints, ComposeUrl } from '@morwalpizvideo/services';

export default async function action({ request, params }: ActionFunctionArgs) {
  const { id } = params;
  const formData = await request.formData();
  const name = formData.get('name') as string;
  const description = formData.get('description') as string;
  const rateLimitPerMinute = parseInt(formData.get('rateLimitPerMinute') as string, 10);
  const expiresAt = formData.get('expiresAt') as string;
  const allowedIpAddressesStr = formData.get('allowedIpAddresses') as string;

  if (!name) {
    return {
      success: false,
      errors: {
        fields: { name: 'Name is required' },
        generics: []
      }
    };
  }

  if (!rateLimitPerMinute || rateLimitPerMinute < 1) {
    return {
      success: false,
      errors: {
        fields: { rateLimitPerMinute: 'Rate limit must be at least 1' },
        generics: []
      }
    };
  }

  const allowedIpAddresses = allowedIpAddressesStr
    ? allowedIpAddressesStr.split('\n').map(ip => ip.trim()).filter(ip => ip.length > 0)
    : [];

  const payload = {
    name,
    description: description || '',
    rateLimitPerMinute,
    expiresAt: expiresAt || null,
    allowedIpAddresses
  };

  try {
    if (id) {
      // Update existing API key
      await put(ComposeUrl(endpoints.APIKEYS_DETAIL, { id: encodeURIComponent(id) }), { ...payload, id });
      return redirect('/keys');
    } else {
      // Create new API key - returns the unhashed key
        const response = await post(endpoints.APIKEYS, payload);
      // Return the response with the new key to display it
      return {
        success: true,
        data: response
      };
    }
  } catch (error) {
    return {
      success: false,
      errors: {
        generics: [(error as Error).message || 'An unexpected error occurred']
      }
    };
  }
}