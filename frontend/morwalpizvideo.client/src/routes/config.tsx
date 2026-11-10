import Root from './layout/root';
import ErrorPage from '../error-page';
import ErrorPageRoot from '../error-page-root';
import Matches from './matches/Component';
import Pages from './pages/Component';
import Links from './links/Component';
import Accessories from './accessories/Component';
import Sponsors from './sponsors/Component';
import Calendar from './calendar/Component';
import SponsorVideo from './system/sponsors-video';
import Index from './home/Component';
import Compilations from './compilations/Component';
import CustomForm from './customForms/Component';
import matchLoader from './matches/loader';
import homeLoader, { homeShouldRevalidate } from './home/loader';
import pageLoader from './pages/loader';
import accessoryLoader from './accessories/loader';
import sponsorsLoader from './sponsors/loader';
import sponsorsAction from './sponsors/action';
import calendarLoader from './calendar/loader';
import streamLoader from './stream/loader';
import compilationsLoader from './compilations/loader';
import customFormLoader from './customForms/loader';
import CookiePolicy from './system/cookie-policy';
import Privacy from './system/privacy';
import Terms from './system/terms';
import Bluetooth from './system/bluetooth';
import Stream from './stream/Component';
import QuickLinks from './quickLinks/Component';
import quickLinksLoader from './quickLinks/loader';
import ChannelNews from './channelNews/Component';
import channelNewsLoader from './channelNews/loader';
import type { RouteObject } from 'react-router';
import Newsletter from './newsletter/Component';
import PushSettings from './pushSettings/Component';
import Survey from './survey/Component';
import surveyLoader from './survey/loader';
import { BlogList, BlogArticle, blogLoader, articleLoader } from './blog';
import Gallery from './gallery/Component';
import galleryLoader from './gallery/loader';
import { Login, Profile, Register } from './account/Components';

export const routes: RouteObject[] = [
  {
    path: 'quick-links/:url',
    loader: quickLinksLoader,
    element: <QuickLinks />,
    errorElement: <ErrorPage />,
  },
  {
    path: '/',
    element: <Root />,
    errorElement: <ErrorPageRoot />,
    children: [
      {
        errorElement: <ErrorPage />,
        children: [
          {
            path: 'gallery/:slug',
            loader: galleryLoader,
            element: <Gallery />,
          },
          {
            path: 'channel-news/:idOrSlug',
            loader: channelNewsLoader,
            element: <ChannelNews />,
          },
          {
            path: 'compilations/:compilationUrl',
            loader: compilationsLoader,
            element: <Compilations />,
          },
          {
            path: 'forms/:formUrl',
            loader: customFormLoader,
            element: <CustomForm />,
          },
          {
            path: 'survey/:surveyUrl',
            loader: surveyLoader,
            element: <Survey />,
          },
          {
            index: true,
            loader: homeLoader,
            shouldRevalidate: homeShouldRevalidate,
            element: <Index />,
          },
          {
            path: 'matches/:matchId',
            loader: matchLoader,
            element: <Matches />,
          },
          {
            path: 'pages/:url',
            loader: pageLoader,
            element: <Pages />,
          },
          { path: 'blog', loader: blogLoader, element: <BlogList /> },
          { path: 'blog/:slug', loader: articleLoader, element: <BlogArticle /> },
          { path: 'account/login', element: <Login /> },
          { path: 'account/register', element: <Register /> },
          { path: 'account/profile', element: <Profile /> },
          {
            path: 'bluetooth',
            element: <Bluetooth />,
          },
          {
            path: 'attrezzatura',
            loader: accessoryLoader,
            element: <Accessories />,
          },
          {
            path: 'sponsors',
            loader: sponsorsLoader,
            action: sponsorsAction,
            element: <Sponsors />,
          },
          {
            path: 'sponsors-video',
            element: <SponsorVideo />,
          },
          {
            path: 'links',
            element: <Links />,
          },
          {
            path: 'calendar',
            loader: calendarLoader,
            element: <Calendar />,
          },
          {
            path: 'cookie-policy',
            element: <CookiePolicy />,
          },
          {
            path: 'privacy',
            element: <Privacy />,
          },
          {
            path: 'terms',
            element: <Terms />,
          },
          {
            path: 'newsletter',
            element: <Newsletter />,
          },
          {
            path: 'notifiche',
            element: <PushSettings />,
          },
          {
            path: 'stream',
            loader: streamLoader,
            element: <Stream />,
          },
        ],
      },
    ],
  },
];
