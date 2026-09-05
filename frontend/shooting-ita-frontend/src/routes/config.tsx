import RootShell from './root';
import HomeRoute from './home/Component';
import { homeLoader } from './home/loader';
import VideoRoute from './video/Component';
import LatestRoute from './latest/Component';
import { latestLoader } from './latest/loader';
import ExclusivesRoute from './exclusives/Component';
import { exclusivesLoader } from './exclusives/loader';
import PopularRoute from './popular/Component';
import { popularLoader } from './popular/loader';
import QuickLinkRoute from './quickLink/Component';
import { quickLinkLoader } from './quickLink/loader';
import ChannelNewsDetail from './channelNews/Component';
import { channelNewsLoader } from './channelNews/loader';
import FaqPage from './faq/Component';
import { faqLoader } from './faq/loader';

export const routes = [{ path: '/', element: <RootShell />, children: [
    { index: true, element: <HomeRoute />, loader: homeLoader },
    { path: 'video/:youtubeId', element: <VideoRoute /> },
    { path: 'latest', element: <LatestRoute />, loader: latestLoader },
    { path: 'exclusives', element: <ExclusivesRoute />, loader: exclusivesLoader },
    { path: 'popular', element: <PopularRoute />, loader: popularLoader },
    { path: 'quick-link/:custom-linktree', element: <QuickLinkRoute />, loader: quickLinkLoader },
    { path: 'channel-news/:idOrSlug', element: <ChannelNewsDetail />, loader: channelNewsLoader },
    { path: 'faq', element: <FaqPage />, loader: faqLoader },
    { path: '*', element: <div>404 Not Found</div> },
] }];