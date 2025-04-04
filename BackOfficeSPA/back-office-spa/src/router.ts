import { createBrowserRouter, Outlet } from 'react-router';
import React from 'react';
import Home from './routes/Home';
import PrimaryLayout from './layouts/PrimaryLayout';
import QueryLinkDetail from './routes/queryLinks/detail';
import QueryLinkEdit from './routes/queryLinks/edit';
import QueryLinkCreate from './routes/queryLinks/create';
import QueryLinks from './routes/queryLinks/index';
import ShortLinks from './routes/shortLinks/index';
import ShortLinkDetail from './routes/shortLinks/detail';
import ShortLinkEdit from './routes/shortLinks/edit';
import ShortLinkCreate from './routes/shortLinks/create';
import ChannelLinks from './routes/channels/index';
import ChannelDetail from './routes/channels/detail';
import ChannelEdit from './routes/channels/edit';
import ChannelCreate from './routes/channels/create';
import Categories from './routes/categories/index';
import CategoryDetail from './routes/categories/detail';
import CategoryEdit from './routes/categories/edit';
import CategoryCreate from './routes/categories/create';
import Videos from './routes/videos/index';
import ImportVideo from './routes/videos/import';
import TranslateVideo from './routes/videos/translate';
import CreateRoot from './routes/videos/create-root';
import CreateSubVideo from './routes/videos/create-sub-video';
import SwapThumbnail from './routes/videos/swap-thumbnail';
import ConvertToRoot from './routes/videos/convert-to-root';
import ErrorBoundary from './components/ErrorBoundary';

export default createBrowserRouter([
  {
    path: '/',
    Component: PrimaryLayout,
    children: [
      { index: true, path: '', Component: Home, errorElement: React.createElement(ErrorBoundary) },
      {
        path: 'querylinks',
        Component: Outlet,
        action: QueryLinks.Action,
        errorElement: React.createElement(ErrorBoundary),
        children: [
          { index: true, path: '', loader: QueryLinks.Loader, Component: QueryLinks.Component },
          { path: 'create', Component: QueryLinkCreate.Component, action: QueryLinkCreate.Action },
          {
            path: ':id',
            Component: Outlet,

            children: [
              {
                index: true,
                path: '',
                loader: QueryLinkDetail.Loader,
                Component: QueryLinkDetail.Component,
              },
              {
                path: 'edit',
                loader: QueryLinkDetail.Loader,
                action: QueryLinkEdit.Action,
                Component: QueryLinkEdit.Component,
              },
            ],
          },
        ],
      },
      {
        path: 'shortlinks',
        Component: Outlet,
        action: ShortLinks.Action,
        errorElement: React.createElement(ErrorBoundary),
        children: [
          { index: true, path: '', loader: ShortLinks.Loader, Component: ShortLinks.Component },
          { path: 'create', Component: ShortLinkCreate.Component, action: ShortLinkCreate.Action },
          {
            path: ':id',
            Component: Outlet,
            children: [
              {
                index: true,
                path: '',
                loader: ShortLinkDetail.Loader,
                Component: ShortLinkDetail.Component,
              },
              {
                path: 'edit',
                loader: ShortLinkDetail.Loader,
                action: ShortLinkEdit.Action,
                Component: ShortLinkEdit.Component,
              },
            ],
          },
        ],
      },
      {
        path: 'channels',
        Component: Outlet,
        action: ChannelLinks.Action,
        errorElement: React.createElement(ErrorBoundary),
        children: [
          { index: true, path: '', loader: ChannelLinks.Loader, Component: ChannelLinks.Component },
          { path: 'create', Component: ChannelCreate.Component, action: ChannelCreate.Action },
          {
            path: ':id',
            Component: Outlet,
            children: [
              {
                index: true,
                path: '',
                loader: ChannelDetail.Loader,
                Component: ChannelDetail.Component,
              },
              {
                path: 'edit',
                loader: ChannelDetail.Loader,
                action: ChannelEdit.Action,
                Component: ChannelEdit.Component,
              },
            ],
          },
        ],
      },
      {
        path: 'categories',
        Component: Outlet,
        action: Categories.Action,
        errorElement: React.createElement(ErrorBoundary),
        children: [
          { index: true, path: '', loader: Categories.Loader, Component: Categories.Component },
          { path: 'create', Component: CategoryCreate.Component, action: CategoryCreate.Action },
          {
            path: ':id',
            Component: Outlet,
            children: [
              {
                index: true,
                path: '',
                loader: CategoryDetail.Loader,
                Component: CategoryDetail.Component,
              },
              {
                path: 'edit',
                loader: CategoryDetail.Loader,
                action: CategoryEdit.Action,
                Component: CategoryEdit.Component,
              },
            ],
          },
        ],
      },
      {
        path: 'videos',
        Component: Outlet,
        errorElement: React.createElement(ErrorBoundary),
        children: [
          { index: true, path: '', Component: Videos.Component },
          { path: 'import', Component: ImportVideo.Component, loader: ImportVideo.loader, action: ImportVideo.Action },
          { path: 'translate', Component: TranslateVideo.Component, action: TranslateVideo.Action },
          { path: 'create-root', Component: CreateRoot.Component, loader: CreateRoot.loader, action: CreateRoot.action },
          { path: 'create-sub-video', Component: CreateSubVideo.Component, loader: CreateSubVideo.loader, action: CreateSubVideo.action },
          { path: 'swap-thumbnail', Component: SwapThumbnail.Component, action: SwapThumbnail.Action,
            loader: SwapThumbnail.Loader
           },
          {
            path: 'convert-to-root',
            action: ConvertToRoot.Action,
            loader: ConvertToRoot.Loader,
            Component: ConvertToRoot.Component,
          },
        ],
      },
    ],
  },
]);
