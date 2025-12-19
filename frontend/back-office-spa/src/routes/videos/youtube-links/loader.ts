import { fetchMatches } from '../../../services/matchesService';

export default async function loader() {
  try {
    const matches = await fetchMatches();
    return { matches };
  } catch (error) {
    console.error('Failed to load matches:', error);
    return { matches: [] };
  }
}
