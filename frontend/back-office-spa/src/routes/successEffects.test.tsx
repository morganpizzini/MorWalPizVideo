import React, { StrictMode } from 'react';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { MemoryRouter, useFetcher, useLoaderData, useNavigate, useParams } from 'react-router';
import { useToast } from '@components/ToastNotification/ToastContext';
import CategoryForm from './categories/form/Component';
import ChannelNewsForm from './channelNews/form/Component';
import QuickLinksForm from './quickLinks/form/Component';
import SponsorForm from './sponsors/form/Component';
import ShortLinkForm from './shortLinks/form/Component';
import CompilationForm from './compilations/form/Component';
import ChannelForm from './channels/form/Component';
import ProductForm from './products/form/Component';
import ProductCategoryForm from './productCategories/form/Component';
import CreateQueryLink from './queryLinks/create/Component';
import EditQueryLink from './queryLinks/edit/Component';
import CreateCalendarEvent from './calendarEvents/create/Component';
import EditCalendarEvent from './calendarEvents/edit/Component';
import CreateConfiguration from './morwalpizconfigurations/create/Component';
import EditConfiguration from './morwalpizconfigurations/edit/Component';
import InsightTopicForm from './insights/form/Component';
import InsightNewsReview from './insights/news/Component';
import NavigationPage from './navigation/Component';
import ImageUpload from './images/upload/Component';
import MultipleImageUpload from './images/upload-multiple/Component';
import TranslateVideo from './videos/translate/Component';
import FaqForm from './faq/form/Component';

vi.mock('react-router', async () => ({
  ...(await vi.importActual<typeof import('react-router')>('react-router')),
  useFetcher: vi.fn(),
  useLoaderData: vi.fn(),
  useNavigate: vi.fn(),
  useParams: vi.fn(),
}));
vi.mock('@components/ToastNotification/ToastContext', () => ({ useToast: vi.fn() }));
vi.mock('@/services/matchesService', () => ({ fetchMatches: vi.fn(async () => []) }));
vi.mock('@morwalpizvideo/services', async () => ({
  ...(await vi.importActual<typeof import('@morwalpizvideo/services')>('@morwalpizvideo/services')),
  get: vi.fn(async () => []),
  fetchProductCategories: vi.fn(async () => []),
  fetchCalendarCategories: vi.fn(async () => []),
}));

const configuration = {
  id: 'config-1',
  key: 'key',
  value: 'value',
  type: 'string',
  description: '',
};
const calendarEvent = {
  title: 'Event',
  categories: [],
  startDate: '2026-01-01',
  endDate: '2026-01-02',
};
const newsItem = {
  topicId: 'topic-1',
  starRating: 1,
  status: 0,
  reviewReason: '',
  title: 'News',
  summary: '',
  aiRelevanceScore: 0.5,
  discoveredAt: '2026-01-01',
};
interface Variant {
  name: string;
  Component: React.ComponentType;
  loader: unknown;
  target?: string;
  toast?: boolean;
}
const variants: Variant[] = [
  { name: 'categories', Component: CategoryForm, loader: null, target: '/categories' },
  { name: 'channelNews', Component: ChannelNewsForm, loader: null, target: '/channelnews' },
  {
    name: 'quickLinks',
    Component: QuickLinksForm,
    loader: { quickLinks: null },
    target: '/quicklinks',
  },
  { name: 'sponsors', Component: SponsorForm, loader: null, target: '/sponsors' },
  { name: 'shortLinks', Component: ShortLinkForm, loader: null, target: '..' },
  { name: 'compilations', Component: CompilationForm, loader: null, target: '/compilations' },
  { name: 'channels', Component: ChannelForm, loader: null, target: '..' },
  {
    name: 'products',
    Component: ProductForm,
    loader: { product: null, categories: [] },
    target: '/products',
  },
  {
    name: 'productCategories',
    Component: ProductCategoryForm,
    loader: { productCategory: null },
    target: '/productcategories',
  },
  { name: 'queryLinks create', Component: CreateQueryLink, loader: null, target: '..' },
  {
    name: 'queryLinks edit',
    Component: EditQueryLink,
    loader: { title: 'Query', value: 'value' },
    target: '..',
  },
  {
    name: 'calendarEvents create',
    Component: CreateCalendarEvent,
    loader: null,
    target: '/calendarEvents',
  },
  {
    name: 'calendarEvents edit',
    Component: EditCalendarEvent,
    loader: { calendarEvent, categories: [] },
    target: '/calendarEvents/Event',
  },
  {
    name: 'configurations create',
    Component: CreateConfiguration,
    loader: null,
    target: '/morwalpizconfigurations',
  },
  {
    name: 'configurations edit',
    Component: EditConfiguration,
    loader: configuration,
    target: '/morwalpizconfigurations/config-1',
  },
  { name: 'insights form', Component: InsightTopicForm, loader: null, target: '/insights' },
  {
    name: 'insights news',
    Component: InsightNewsReview,
    loader: newsItem,
    target: '/insights/topic-1',
  },
  { name: 'navigation', Component: NavigationPage, loader: { navigation: null, pages: [] } },
  { name: 'image upload', Component: ImageUpload, loader: { matches: [] } },
  { name: 'multiple image upload', Component: MultipleImageUpload, loader: { matches: [] } },
  { name: 'translation', Component: TranslateVideo, loader: null },
  {
    name: 'faq',
    Component: FaqForm,
    loader: { faq: null, categories: [] },
    target: '/faq',
    toast: false,
  },
];

const fetcher = {
  state: 'idle',
  data: undefined as unknown,
  submit: vi.fn(),
  Form: ({ children, ...props }: React.ComponentProps<'form'>) => (
    <form {...props}>{children}</form>
  ),
};
const show = vi.fn();
const navigate = vi.fn();

beforeEach(() => {
  vi.clearAllMocks();
  fetcher.state = 'idle';
  fetcher.data = undefined;
  vi.mocked(useFetcher).mockReturnValue(fetcher as never);
  vi.mocked(useParams).mockReturnValue({});
  vi.mocked(useToast).mockReturnValue({ show });
  vi.mocked(useNavigate).mockReturnValue(navigate);
});

function mountVariant(variant: Variant) {
  vi.mocked(useLoaderData).mockReturnValue(variant.loader as never);
  const { Component } = variant;
  const element = () => (
    <StrictMode>
      <MemoryRouter>
        <Component />
      </MemoryRouter>
    </StrictMode>
  );
  const view = render(element());
  return { ...view, refresh: () => view.rerender(element()) };
}

describe('form success effects', () => {
  it.each(variants)(
    '$name processes only new idle responses, including identical payloads',
    async variant => {
      fetcher.data = { success: true };
      fetcher.state = 'submitting';
      const { refresh } = mountVariant(variant);
      await act(async () => {});
      expect(show).not.toHaveBeenCalled();
      expect(navigate).not.toHaveBeenCalled();
      fetcher.state = 'loading';
      refresh();
      expect(show).not.toHaveBeenCalled();
      expect(navigate).not.toHaveBeenCalled();
      fetcher.state = 'idle';
      refresh();
      expect(show).toHaveBeenCalledTimes(variant.toast === false ? 0 : 1);
      expect(navigate).toHaveBeenCalledTimes(variant.target ? 1 : 0);
      if (variant.target) expect(navigate).toHaveBeenCalledWith(variant.target);

      const nextShow = vi.fn();
      const nextNavigate = vi.fn();
      vi.mocked(useToast).mockReturnValue({ show: nextShow });
      vi.mocked(useNavigate).mockReturnValue(nextNavigate);
      vi.mocked(useParams).mockReturnValue({
        id: 'changed',
        productId: 'changed',
        categoryId: 'changed',
      });
      refresh();
      fetcher.state = 'submitting';
      refresh();
      fetcher.state = 'idle';
      refresh();
      expect(nextShow).not.toHaveBeenCalled();
      expect(nextNavigate).not.toHaveBeenCalled();
      fetcher.data = { success: false };
      refresh();
      fetcher.data = undefined;
      refresh();
      expect(nextShow).not.toHaveBeenCalled();
      expect(nextNavigate).not.toHaveBeenCalled();
      fetcher.data = { success: true };
      refresh();
      expect(nextShow).toHaveBeenCalledTimes(variant.toast === false ? 0 : 1);
      expect(nextNavigate).toHaveBeenCalledTimes(variant.target ? 1 : 0);
    }
  );

  it('does not replay an initially idle success under StrictMode', () => {
    fetcher.data = { success: true };
    mountVariant(variants.find(variant => variant.name === 'channelNews')!);
    expect(show).toHaveBeenCalledTimes(1);
    expect(navigate).toHaveBeenCalledTimes(1);
  });

  it('shows each channel cache warning once alongside success', () => {
    fetcher.data = {
      success: true,
      cacheInvalidation: { status: 'failed', message: 'Cache warning' },
    };
    const { refresh } = mountVariant(variants.find(variant => variant.name === 'channels')!);
    refresh();
    vi.mocked(useToast).mockReturnValue({ show });
    refresh();
    expect(show).toHaveBeenCalledTimes(2);
    expect(show).toHaveBeenNthCalledWith(2, 'Warning', 'Cache warning', { variant: 'warning' });
    expect(navigate).toHaveBeenCalledTimes(1);
    fetcher.data = {
      success: true,
      cacheInvalidation: { status: 'failed', message: 'Cache warning' },
    };
    refresh();
    expect(show).toHaveBeenCalledTimes(4);
    expect(navigate).toHaveBeenCalledTimes(2);
  });

  it('resets translation once without wiping the next input', () => {
    const { refresh } = mountVariant(variants.find(variant => variant.name === 'translation')!);
    const input = screen.getByPlaceholderText('Enter the YouTube video ID');
    fireEvent.change(input, { target: { value: 'first' } });
    fetcher.data = { success: true };
    fetcher.state = 'loading';
    refresh();
    expect(input).toHaveValue('first');
    fetcher.state = 'idle';
    refresh();
    expect(input).toHaveValue('');
    fireEvent.change(input, { target: { value: 'second' } });
    vi.mocked(useToast).mockReturnValue({ show });
    refresh();
    fetcher.state = 'submitting';
    refresh();
    fetcher.state = 'idle';
    refresh();
    expect(input).toHaveValue('second');
    expect(show).toHaveBeenCalledTimes(1);
    fetcher.data = { success: true };
    refresh();
    expect(input).toHaveValue('');
    expect(show).toHaveBeenCalledTimes(2);
    expect(navigate).not.toHaveBeenCalled();
  });

  it.each([
    { name: 'image upload', label: 'Image File' },
    { name: 'multiple image upload', label: 'Image Files' },
  ])('$name clears the file input once, retaining the next selection', ({ name, label }) => {
    const { refresh } = mountVariant(variants.find(variant => variant.name === name)!);
    const input = screen.getByLabelText(new RegExp(label)) as HTMLInputElement;
    const valueSetter = vi.fn();
    Object.defineProperty(input, 'value', { configurable: true, get: () => '', set: valueSetter });
    fetcher.data = { success: true };
    refresh();
    expect(valueSetter).toHaveBeenCalledTimes(1);
    vi.mocked(useToast).mockReturnValue({ show });
    refresh();
    fetcher.state = 'submitting';
    refresh();
    fetcher.state = 'idle';
    refresh();
    expect(valueSetter).toHaveBeenCalledTimes(1);
    fetcher.data = { success: true };
    refresh();
    expect(valueSetter).toHaveBeenCalledTimes(2);
    expect(navigate).not.toHaveBeenCalled();
  });

  it('closes a mixed-result modal only once per idle result, including failure', async () => {
    const { container, refresh } = mountVariant(
      variants.find(variant => variant.name === 'queryLinks create')!
    );
    fireEvent.submit(container.querySelector('form')!);
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    fetcher.data = { success: false };
    fetcher.state = 'loading';
    refresh();
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    fetcher.state = 'idle';
    refresh();
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    fireEvent.submit(container.querySelector('form')!);
    vi.mocked(useToast).mockReturnValue({ show });
    refresh();
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(show).not.toHaveBeenCalled();
    expect(navigate).not.toHaveBeenCalled();
    fetcher.data = { success: true };
    refresh();
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(show).toHaveBeenCalledTimes(1);
    expect(navigate).toHaveBeenCalledTimes(1);
  });
});
