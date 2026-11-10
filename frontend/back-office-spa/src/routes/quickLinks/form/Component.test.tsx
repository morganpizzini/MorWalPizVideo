import { beforeEach, describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QuickLinkKind, type QuickLinks } from '@morwalpizvideo/models';
import { useResolvedLoaderData } from '@/router/asyncData';
import { useFetcher, useNavigate, useParams } from 'react-router';
import { render } from '../../../test/test-utils';
import Component from './Component';

vi.mock('@/router/asyncData', () => ({
  useResolvedLoaderData: vi.fn(),
}));

vi.mock('react-router', async () => {
  const actual = await vi.importActual<typeof import('react-router')>('react-router');
  return {
    ...actual,
    useFetcher: vi.fn(),
    useNavigate: vi.fn(),
    useParams: vi.fn(),
  };
});

const quickLinks: QuickLinks = {
  title: 'Links',
  subtitle: '',
  url: 'links',
  links: [{ kind: QuickLinkKind.External, targetUrl: 'https://example.test/', title: '' }],
};

const fetcher = {
  state: 'idle',
  data: undefined,
  Form: ({ children }: { children: React.ReactNode }) => <form>{children}</form>,
};

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(useResolvedLoaderData).mockReturnValue({ quickLinks });
  vi.mocked(useFetcher).mockReturnValue(fetcher as never);
  vi.mocked(useNavigate).mockReturnValue(vi.fn());
  vi.mocked(useParams).mockReturnValue({});
});

describe('QuickLinks form', () => {
  it('keeps focus while typing a link target URL', async () => {
    const user = userEvent.setup();
    render(<Component />);

    const targetUrl = screen.getByDisplayValue('https://example.test/');
    await user.type(targetUrl, 'path');

    expect(targetUrl).toHaveValue('https://example.test/path');
    expect(targetUrl).toHaveFocus();
  });

  it('previews the selected YouTube icon and keeps its kind in the form payload', async () => {
    const user = userEvent.setup();
    const { container } = render(<Component />);

    await user.selectOptions(screen.getByLabelText('Kind'), String(QuickLinkKind.YouTube));

    expect(container.querySelector('.quick-links-kind-preview [data-icon="youtube"]')).toBeInTheDocument();
    expect(container.querySelector<HTMLInputElement>('input[name="links"]')?.value).toContain('"kind":5');
  });
});
