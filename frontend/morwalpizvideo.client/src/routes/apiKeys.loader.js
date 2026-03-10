import { getAllApiKeys } from '../services/apiKeys';

export default async function loader() {
  try {
    const apiKeys = await getAllApiKeys();
    return { apiKeys };
  } catch (error) {
    console.error('Error loading API keys:', error);
    throw new Response('Failed to load API keys', { status: 500 });
  }
}