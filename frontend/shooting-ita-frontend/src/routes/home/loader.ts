import type { LoaderFunction } from 'react-router-dom';
import { loadMatchesWithChannels } from '../../services/shootingItaVideoService';
import { deriveFeatured, deriveExclusives } from '../../utils/deriveCategories';
import { getPublicChannelNews } from '@morwalpizvideo/services';

export const homeLoader: LoaderFunction = async () => {
    const [{ matches, ownerMap }, channelNews] = await Promise.all([
        loadMatchesWithChannels(),
        getPublicChannelNews(),
    ]);
    const featured = deriveFeatured(matches);
    const exclusiveRail = deriveExclusives(
        matches,
        import.meta.env.VITE_EXCLUSIVE_CATEGORY_ID as string | undefined,
    );
    return { featured, exclusiveRail, ownerMap, channelNews };
};
