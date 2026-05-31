import type { LoaderFunctionArgs } from 'react-router';
import { getApiKeyById } from '../services/apiKeys';

export default async function loader({ params }: LoaderFunctionArgs) {
  try {
    const apiKey = await getApiKeyById(params.id as string);
    return { apiKey };
  } catch (error) {
    console.error('Error loading API key:', error);
    throw new Response('API key not found', { status: 404 });
  }
}