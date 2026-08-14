import React from 'react'
import ReactDOM from 'react-dom/client'
import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import {
  GoogleReCaptchaProvider
} from 'react19-google-recaptcha-v3';
import 'bootstrap/dist/css/bootstrap.min.css';
import './index.css'

import RootShell from './routes/root';
import HomeRoute from './routes/home/Component';
import { homeLoader } from './routes/home/loader';
import VideoRoute from './routes/video/Component';
import LatestRoute from './routes/latest/Component';
import { latestLoader } from './routes/latest/loader';
import ExclusivesRoute from './routes/exclusives/Component';
import { exclusivesLoader } from './routes/exclusives/loader';
import PopularRoute from './routes/popular/Component';
import { popularLoader } from './routes/popular/loader';
import QuickLinkRoute from './routes/quickLink/Component';
import { quickLinkLoader } from './routes/quickLink/loader';
import ChannelNewsDetail from './routes/channelNews/Component';
import { channelNewsLoader } from './routes/channelNews/loader';

const router = createBrowserRouter([
  {
    path: '/',
    element: <RootShell />,
    children: [
      { index: true, element: <HomeRoute />, loader: homeLoader },
      { path: 'video/:youtubeId', element: <VideoRoute /> },
      { path: 'latest', element: <LatestRoute />, loader: latestLoader },
      { path: 'exclusives', element: <ExclusivesRoute />, loader: exclusivesLoader },
      { path: 'popular', element: <PopularRoute />, loader: popularLoader },
      { path: 'quick-link/:custom-linktree', element: <QuickLinkRoute />, loader: quickLinkLoader },
      { path: 'channel-news/:idOrSlug', element: <ChannelNewsDetail />, loader: channelNewsLoader },
      { path: '*', element: <div>404 Not Found</div> },
    ],
  },
]);

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <GoogleReCaptchaProvider reCaptchaKey={import.meta.env.VITE_SITE_KEY}>
      <RouterProvider router={router} />
    </GoogleReCaptchaProvider>
  </React.StrictMode>,
)
