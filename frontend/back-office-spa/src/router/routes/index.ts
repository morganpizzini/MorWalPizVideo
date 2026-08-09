import { Outlet } from 'react-router';
import Home from '../../routes/Home';
import { createErrorElement, createRouteGroup } from '../utils';
import type { RouteConfig } from '../types';

import { getRoutePermissions } from '../../authorization/permissions';
import { withActionPermission, withPermission } from '../guards';
import { protectedRoutes as lazyProtectedRoutes } from './lazy-index';

const legacyRouteModule = {} as any;
const QueryLinks = legacyRouteModule;
const QueryLinkDetail = legacyRouteModule;
const QueryLinkEdit = legacyRouteModule;
const QueryLinkCreate = legacyRouteModule;
const ShortLinks = legacyRouteModule;
const ShortLinkDetail = legacyRouteModule;
const ShortLinkForm = legacyRouteModule;
const ChannelLinks = legacyRouteModule;
const ChannelDetail = legacyRouteModule;
const ChannelForm = legacyRouteModule;
const Categories = legacyRouteModule;
const CategoryDetail = legacyRouteModule;
const CategoryEdit = legacyRouteModule;
const CategoryCreate = legacyRouteModule;
const Videos = legacyRouteModule;
const VideoDetail = legacyRouteModule;
const VideoEdit = legacyRouteModule;
const ImportVideo = legacyRouteModule;
const TranslateVideo = legacyRouteModule;
const ImagesHome = legacyRouteModule;
const ImageUpload = legacyRouteModule;
const MultipleImageUpload = legacyRouteModule;
const CalendarEvents = legacyRouteModule;
const CalendarEventDetail = legacyRouteModule;
const CalendarEventEdit = legacyRouteModule;
const CalendarEventCreate = legacyRouteModule;
const MorWalPizConfigurations = legacyRouteModule;
const MorWalPizConfigurationDetail = legacyRouteModule;
const MorWalPizConfigurationEdit = legacyRouteModule;
const MorWalPizConfigurationCreate = legacyRouteModule;
const ProductCategories = legacyRouteModule;
const ProductCategoryForm = legacyRouteModule;
const Sponsors = legacyRouteModule;
const SponsorCreate = legacyRouteModule;
const SponsorEdit = legacyRouteModule;
const Products = legacyRouteModule;
const ProductDetail = legacyRouteModule;
const ProductForm = legacyRouteModule;
const Compilations = legacyRouteModule;
const CompilationDetail = legacyRouteModule;
const CompilationForm = legacyRouteModule;
const CustomForms = legacyRouteModule;
const CustomFormDetail = legacyRouteModule;
const CustomFormForm = legacyRouteModule;
const Insights = legacyRouteModule;
const InsightDetail = legacyRouteModule;
const InsightForm = legacyRouteModule;
const InsightNews = legacyRouteModule;
const scanNewsAction = legacyRouteModule;
const ApiKeys = legacyRouteModule;
const ApiKeyDetail = legacyRouteModule;
const ApiKeyForm = legacyRouteModule;
const Diagnostics = legacyRouteModule;
const RbacManagement = legacyRouteModule;
const RbacUsersPage = legacyRouteModule;
const RbacUserCreatePage = legacyRouteModule;
const RbacUserDetailPage = legacyRouteModule;
const RbacUserEditPage = legacyRouteModule;
const RbacGroupsPage = legacyRouteModule;
const RbacGroupCreatePage = legacyRouteModule;
const RbacGroupDetailPage = legacyRouteModule;
const RbacGroupEditPage = legacyRouteModule;
const Profile = legacyRouteModule;

const routeDefinitions: RouteConfig[] = [
  {
    index: true,
    path: '',
    Component: Home,
    errorElement: createErrorElement()
  },

  { path: 'diagnostics', Component: Diagnostics, errorElement: createErrorElement() },
  { path: 'profile', Component: Profile.Component, errorElement: createErrorElement() },
  {
    path: 'rbac',
    Component: Outlet,
    errorElement: createErrorElement(),
    children: [
      { index: true, path: '', Component: RbacManagement },
      { path: 'users', Component: RbacUsersPage },
      { path: 'users/create', Component: RbacUserCreatePage },
      { path: 'users/:id', Component: RbacUserDetailPage },
      { path: 'users/:id/edit', Component: RbacUserEditPage },
      { path: 'groups', Component: RbacGroupsPage },
      { path: 'groups/create', Component: RbacGroupCreatePage },
      { path: 'groups/:id', Component: RbacGroupDetailPage },
      { path: 'groups/:id/edit', Component: RbacGroupEditPage },
    ],
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
      { path: 'create', loader: ShortLinkForm.Loader, Component: ShortLinkForm.Component, action: ShortLinkForm.Action },
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
            loader: ShortLinkForm.Loader,
            action: ShortLinkForm.Action,
            Component: ShortLinkForm.Component,
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
      { path: 'create', loader: ChannelForm.Loader, Component: ChannelForm.Component, action: ChannelForm.Action },
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
            loader: ChannelForm.Loader,
            action: ChannelForm.Action,
            Component: ChannelForm.Component,
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
      { path: 'create', loader: ProductCategoryForm.Loader, Component: ProductCategoryForm.Component, action: ProductCategoryForm.Action },
      {
        path: ':categoryId/edit',
        loader: ProductCategoryForm.Loader,
        action: ProductCategoryForm.Action,
        Component: ProductCategoryForm.Component,
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
      { path: 'create', loader: ProductForm.Loader, Component: ProductForm.Component, action: ProductForm.Action },
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
            loader: ProductForm.Loader,
            action: ProductForm.Action,
            Component: ProductForm.Component,
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

function protectRoute(route: RouteConfig, parentPath = ''): RouteConfig {
  const currentPath = [parentPath, route.path].filter(Boolean).join('/');
  const loaderPermissions = getRoutePermissions(currentPath, false);
  const actionPermissions = getRoutePermissions(currentPath, true);
  const loader = typeof route.loader === 'function' ? route.loader : undefined;
  const action = typeof route.action === 'function' ? route.action : undefined;

  return ({
    ...route,
    loader: withPermission(loaderPermissions, loader),
    action: action ? withActionPermission(actionPermissions, action) : undefined,
    children: route.children?.map(child => protectRoute(child, currentPath)),
  }) as RouteConfig;
}

export const protectedRoutes = lazyProtectedRoutes;

void routeDefinitions;
void protectRoute;
