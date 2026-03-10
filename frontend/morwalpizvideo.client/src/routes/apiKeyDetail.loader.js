import { getApiKeyById } from '../services/apiKeys';

export default async function loader({ params }) {
  try {
    const apiKey = await getApiKeyById(params.id);
    return { apiKey };
  } catch (error) {
    console.error('Error loading API key:', error);
    throw new Response('API key not found', { status: 404 });
  }
}