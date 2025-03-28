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
    ],
  },
]);
