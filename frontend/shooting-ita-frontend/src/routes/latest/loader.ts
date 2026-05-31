import type { LoaderFunction } from 'react-router-dom';
import { loadMatchesWithChannels } from '../../services/shootingItaVideoService';
import { deriveLatest } from '../../utils/deriveCategories';

export const latestLoader: LoaderFunction = async () => {
    const { matches } = await loadMatchesWithChannels();
    return { items: deriveLatest(matches) };
};
