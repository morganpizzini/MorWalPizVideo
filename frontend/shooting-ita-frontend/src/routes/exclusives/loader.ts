import type { LoaderFunction } from 'react-router-dom';
import { loadMatchesWithChannels } from '../../services/shootingItaVideoService';
import { deriveExclusives } from '../../utils/deriveCategories';

export const exclusivesLoader: LoaderFunction = async () => {
    const { matches } = await loadMatchesWithChannels();
    const items = deriveExclusives(matches, import.meta.env.VITE_EXCLUSIVE_CATEGORY_ID as string | undefined);
    return { items };
};
