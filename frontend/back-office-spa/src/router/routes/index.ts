import { Outlet } from 'react-router';
import Home from '../../routes/Home';
import { createErrorElement, createRouteGroup } from '../utils';
import type { RouteConfig } from '../types';

// Import route modules using default imports (they export { Component, action, loader } as default)
import QueryLinks from '../../routes/queryLinks/index';
import QueryLinkDetail from '../../routes/queryLinks/detail';
import QueryLinkEdit from '../../routes/queryLinks/edit';
import QueryLinkCreate from '../../routes/queryLinks/create';

import ShortLinks from '../../routes/shortLinks/index';
import ShortLinkDetail from '../../routes/shortLinks/detail';
import ShortLinkEdit from '../../routes/shortLinks/edit';
import ShortLinkCreate from '../../routes/shortLinks/create';

import ChannelLinks from '../../routes/channels/index';
import ChannelDetail from '../../routes/channels/detail';
import ChannelEdit from '../../routes/channels/edit';
import ChannelCreate from '../../routes/channels/create';

import Categories from '../../routes/categories/index';
import CategoryDetail from '../../routes/categories/detail';
import CategoryEdit from '../../routes/categories/edit';
import CategoryCreate from '../../routes/categories/create';

import Videos from '../../routes/videos/index';
import VideoDetail from '../../routes/videos/detail';
import VideoEdit from '../../routes/videos/edit';
import ImportVideo from '../../routes/videos/import';
import TranslateVideo from '../../routes/videos/translate';
import CreateRoot from '../../routes/videos/create-root';
import CreateSubVideo from '../../routes/videos/create-sub-video';
import SwapThumbnail from '../../routes/videos/swap-thumbnail';
import ConvertToRoot from '../../routes/videos/convert-to-root';
import YouTubeLinksComponent from '../../routes/videos/youtube-links/Component';
import youTubeLinksLoader from '../../routes/videos/youtube-links/loader';

import ImagesHome from '../../routes/images/index';
import ImageUpload from '../../routes/images/upload';
import MultipleImageUpload from '../../routes/images/upload-multiple';

import CalendarEvents from '../../routes/calendarEvents/index';
import CalendarEventDetail from '../../routes/calendarEvents/detail';
import CalendarEventEdit from '../../routes/calendarEvents/edit';
import CalendarEventCreate from '../../routes/calendarEvents/create';

import MorWalPizConfigurations from '../../routes/morwalpizconfigurations/index';
import MorWalPizConfigurationDetail from '../../routes/morwalpizconfigurations/detail';
import MorWalPizConfigurationEdit from '../../routes/morwalpizconfigurations/edit';
import MorWalPizConfigurationCreate from '../../routes/morwalpizconfigurations/create';

// Import with namespace for components that use named exports
import * as ProductCategories from '../../routes/productCategories/index';
import * as ProductCategoryCreate from '../../routes/productCategories/create';
import * as ProductCategoryEdit from '../../routes/productCategories/edit';

import * as Sponsors from '../../routes/sponsors/index';
import * as SponsorCreate from '../../routes/sponsors/create';
import * as SponsorEdit from '../../routes/sponsors/edit';

import * as Products from '../../routes/products/index';
import * as ProductDetail from '../../routes/products/detail';
import * as ProductCreate from '../../routes/products/create';
import * as ProductEdit from '../../routes/products/edit';

import Compilations from '../../routes/compilations/index';
import CompilationDetail from '../../routes/compilations/detail';
import CompilationForm from '../../routes/compilations/form';

import CustomForms from '../../routes/customForms/index';
import CustomFormDetail from '../../routes/customForms/detail';
import CustomFormForm from '../../routes/customForms/form';

import Insights from '../../routes/insights/index';
import InsightDetail from '../../routes/insights/detail';
import InsightForm from '../../routes/insights/form';
import InsightNews from '../../routes/insights/news';
import scanNewsAction from '../../routes/insights/scan-news/action';

import ApiKeys from '../../routes/apiKeys/index';
import ApiKeyDetail from '../../routes/apiKeys/detail';
import ApiKeyForm from '../../routes/apiKeys/form';

/**
 * Protected routes (require authentication)
 * These routes are rendered within the PrimaryLayout component
 */
export const protectedRoutes: RouteConfig[] = [
  { 
    index: true, 
    path: '', 
    Component: Home, 
    errorElement: createErrorElement() 
  },
  
  // Calendar Events
  createRouteGroup('calendarevents', {
    action: CalendarEvents.Action,
    children: [
      { index: true, path: '', loader: CalendarEvents.Loader, Component: CalendarEvents.Component },
      { path: 'create', Component: CalendarEventCreate.Component, action: CalendarEventCreate.Action },
      {
        path: ':id',
        Component: Outlet,
        children: [
          {
            index: true,
            path: '',
            loader: CalendarEventDetail.Loader,
            Component: CalendarEventDetail.Component,
          },
          {
            path: 'edit',
            loader: CalendarEventDetail.Loader,
            action: CalendarEventEdit.Action,
            Component: CalendarEventEdit.Component,
          },
        ],
      },
    ],
  }),

  // Query Links  
  createRouteGroup('querylinks', {
    action: QueryLinks.Action,
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
  }),

  // Short Links
  createRouteGroup('shortlinks', {
    action: ShortLinks.Action,
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
  }),

  // Channels
  createRouteGroup('channels', {
    action: ChannelLinks.Action,
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
  }),

  // Categories
  createRouteGroup('categories', {
    action: Categories.Action,
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
  }),

  // Videos
  {
    path: 'videos',
    Component: Outlet,
    errorElement: createErrorElement(),
    children: [
      { index: true, path: '', loader: Videos.loader, Component: Videos.Component },
      { path: 'import', Component: ImportVideo.Component, loader: ImportVideo.loader, action: ImportVideo.Action },
      { path: 'translate', Component: TranslateVideo.Component, action: TranslateVideo.Action },
      { path: 'create-root', Component: CreateRoot.Component, loader: CreateRoot.loader, action: CreateRoot.action },
      { path: 'create-sub-video', Component: CreateSubVideo.Component, loader: CreateSubVideo.loader, action: CreateSubVideo.action },
      { path: 'swap-thumbnail', Component: SwapThumbnail.Component, action: SwapThumbnail.Action, loader: SwapThumbnail.Loader },
      { path: 'convert-to-root', action: ConvertToRoot.Action, loader: ConvertToRoot.Loader, Component: ConvertToRoot.Component },
      { path: 'youtube-links', loader: youTubeLinksLoader, Component: YouTubeLinksComponent },
      {
        path: ':id',
        Component: Outlet,
        children: [
          {
            index: true,
            path: '',
            loader: VideoDetail.loader,
            Component: VideoDetail.Component,
          },
          {
            path: 'edit',
            loader: VideoDetail.loader,
            action: VideoEdit.action,
            Component: VideoEdit.Component,
          },
        ],
      },
    ],
  },

  // Images
  {
    path: 'images',
    Component: Outlet,
    errorElement: createErrorElement(),
    children: [
      { index: true, path: '', Component: ImagesHome.Component },
      {
        path: 'upload',
        Component: ImageUpload.Component,
        loader: ImageUpload.loader,
        action: ImageUpload.action,
      },
      {
        path: 'upload-multiple',
        Component: MultipleImageUpload.Component,
        loader: MultipleImageUpload.loader,
        action: MultipleImageUpload.action,
      },
    ],
  },

  // MorWalPiz Configurations
  createRouteGroup('morwalpizconfigurations', {
    action: MorWalPizConfigurations.Action,
    children: [
      { index: true, path: '', loader: MorWalPizConfigurations.Loader, Component: MorWalPizConfigurations.Component },
      { path: 'create', Component: MorWalPizConfigurationCreate.Component, action: MorWalPizConfigurationCreate.Action },
      {
        path: ':id',
        Component: Outlet,
        children: [
          {
            index: true,
            path: '',
            loader: MorWalPizConfigurationDetail.Loader,
            Component: MorWalPizConfigurationDetail.Component,
          },
          {
            path: 'edit',
            loader: MorWalPizConfigurationDetail.Loader,
            action: MorWalPizConfigurationEdit.Action,
            Component: MorWalPizConfigurationEdit.Component,
          },
        ],
      },
    ],
  }),

  // Product Categories
  createRouteGroup('productcategories', {
    action: ProductCategories.action,
    children: [
      { index: true, path: '', loader: ProductCategories.loader, Component: ProductCategories.Component },
      { path: 'create', Component: ProductCategoryCreate.Component, action: ProductCategoryCreate.action },
      {
        path: ':categoryId/edit',
        loader: ProductCategoryEdit.loader,
        action: ProductCategoryEdit.action,
        Component: ProductCategoryEdit.Component,
      },
    ],
  }),

  // Sponsors
  createRouteGroup('sponsors', {
    action: Sponsors.action,
    children: [
      { index: true, path: '', loader: Sponsors.loader, Component: Sponsors.Component },
      { path: 'create', Component: SponsorCreate.Component, action: SponsorCreate.action },
      {
        path: ':sponsorId/edit',
        loader: SponsorEdit.loader,
        action: SponsorEdit.action,
        Component: SponsorEdit.Component,
      },
    ],
  }),

  // Products
  createRouteGroup('products', {
    action: Products.action,
    children: [
      { index: true, path: '', loader: Products.loader, Component: Products.Component },
      { path: 'create', Component: ProductCreate.Component, action: ProductCreate.action },
      {
        path: ':productId',
        Component: Outlet,
        children: [
          {
            index: true,
            path: '',
            loader: ProductDetail.loader,
            Component: ProductDetail.Component,
          },
          {
            path: 'edit',
            loader: ProductEdit.loader,
            action: ProductEdit.action,
            Component: ProductEdit.Component,
          },
        ],
      },
    ],
  }),

  // Compilations
  createRouteGroup('compilations', {
    action: Compilations.Action,
    children: [
      { index: true, path: '', loader: Compilations.Loader, Component: Compilations.Component },
      { path: 'create', loader: CompilationForm.Loader, Component: CompilationForm.Component, action: CompilationForm.Action },
      {
        path: ':id',
        Component: Outlet,
        children: [
          {
            index: true,
            path: '',
            loader: CompilationDetail.Loader,
            Component: CompilationDetail.Component,
          },
          {
            path: 'edit',
            loader: CompilationForm.Loader,
            action: CompilationForm.Action,
            Component: CompilationForm.Component,
          },
        ],
      },
    ],
  }),

  // Custom Forms
  createRouteGroup('customforms', {
    action: CustomForms.Action,
    children: [
      { index: true, path: '', loader: CustomForms.Loader, Component: CustomForms.Component },
      { path: 'create', loader: CustomFormForm.Loader, Component: CustomFormForm.Component, action: CustomFormForm.Action },
      {
        path: ':id',
        Component: Outlet,
        children: [
          {
            index: true,
            path: '',
            loader: CustomFormDetail.Loader,
            Component: CustomFormDetail.Component,
          },
          {
            path: 'edit',
            loader: CustomFormForm.Loader,
            action: CustomFormForm.Action,
            Component: CustomFormForm.Component,
          },
        ],
      },
    ],
  }),

  // Insights
  createRouteGroup('insights', {
    action: Insights.Action,
    children: [
      { index: true, path: '', loader: Insights.Loader, Component: Insights.Component },
      { path: 'create', loader: InsightForm.Loader, Component: InsightForm.Component, action: InsightForm.Action },
      {
        path: 'news/:newsId',
        loader: InsightNews.Loader,
        action: InsightNews.Action,
        Component: InsightNews.Component,
      },
      {
        path: ':id',
        Component: Outlet,
        children: [
          {
            index: true,
            path: '',
            loader: InsightDetail.Loader,
            Component: InsightDetail.Component,
          },
          {
            path: 'edit',
            loader: InsightForm.Loader,
            action: InsightForm.Action,
            Component: InsightForm.Component,
          },
          {
            path: 'scan-news',
            action: scanNewsAction,
          },
        ],
      },
    ],
  }),

  // API Keys
  createRouteGroup('keys', {
    action: ApiKeys.action,
    children: [
      { index: true, path: '', loader: ApiKeys.loader, Component: ApiKeys.component },
      { path: 'create', loader: ApiKeyForm.loader, Component: ApiKeyForm.component, action: ApiKeyForm.action },
      {
        path: ':id',
        Component: Outlet,
        children: [
          {
            index: true,
            path: '',
            loader: ApiKeyDetail.loader,
            Component: ApiKeyDetail.component,
          },
          {
            path: 'edit',
            loader: ApiKeyForm.loader,
            action: ApiKeyForm.action,
            Component: ApiKeyForm.component,
          },
        ],
      },
    ],
  }),
];
