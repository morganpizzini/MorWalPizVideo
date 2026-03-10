import { getApiKeyById } from '../services/apiKeys';

export default async function loader({ params }) {
  // If editing (has id parameter), fetch the API key
  if (params.id) {
    try {
      const apiKey = await getApiKeyById(params.id);
      return { apiKey, isEdit: true };
    } catch (error) {
      console.error('Error loading API key:', error);
      throw new Response('API key not found', { status: 404 });
    }
  }
  
  // Creating new API key
  return { apiKey: null, isEdit: false };
}