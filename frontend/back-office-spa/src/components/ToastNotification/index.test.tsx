import { StrictMode } from 'react';
import { URLSearchParams as NodeURLSearchParams } from 'node:url';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { createMemoryRouter, Outlet, RouterProvider } from 'react-router';
import { ToastProvider } from './index';
import { useToast, type ToastContextProps } from './ToastContext';
import PageForm from '@/routes/pages/form/Component';

describe('ToastProvider', () => {
  it('keeps the context and show identity stable across parent, toast and close renders', async () => {
    const contexts: ToastContextProps[] = [];
    function Consumer() {
      const toast = useToast();
      contexts.push(toast);
      return (
        <button
          onClick={() => toast.show('Success', 'Saved', { autohide: false, variant: 'success' })}
        >
          Notify
        </button>
      );
    }
    const element = () => (
      <StrictMode>
        <ToastProvider>
          <Consumer />
        </ToastProvider>
      </StrictMode>
    );
    const { rerender } = render(element());
    const original = contexts[0];
    fireEvent.click(screen.getByRole('button', { name: 'Notify' }));
    expect(screen.getAllByText('Saved')).toHaveLength(1);
    expect(screen.getByText('Saved').closest('.toast')).toHaveClass('bg-success');
    rerender(element());
    fireEvent.click(screen.getByRole('button', { name: 'Close' }));
    await waitFor(() => expect(screen.queryByText('Saved')).not.toBeInTheDocument());
    rerender(element());
    expect(contexts.every(context => context === original && context.show === original.show)).toBe(
      true
    );
  });

  it('keeps a single page success toast visible after real router navigation', async ({
    onTestFinished,
  }) => {
    vi.stubGlobal('URLSearchParams', NodeURLSearchParams);
    onTestFinished(() => vi.unstubAllGlobals());
    const action = vi.fn(async () => ({ success: true }));
    const router = createMemoryRouter(
      [
        {
          element: (
            <ToastProvider>
              <Outlet />
            </ToastProvider>
          ),
          hydrateFallbackElement: <span>Loading</span>,
          children: [
            { path: '/pages/new', loader: () => null, action, element: <PageForm /> },
            { path: '/pages', element: <h1>Pages destination</h1> },
          ],
        },
      ],
      { initialEntries: ['/pages/new'] }
    );
    const { container } = render(
      <StrictMode>
        <RouterProvider router={router} />
      </StrictMode>
    );
    await screen.findByRole('button', { name: 'Save page' });
    fireEvent.change(container.querySelector('input[name="title"]')!, {
      target: { value: 'About' },
    });
    fireEvent.change(container.querySelector('input[name="url"]')!, { target: { value: 'about' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save page' }));
    await waitFor(() => expect(action).toHaveBeenCalledTimes(1));
    await screen.findByRole('heading', { name: 'Pages destination' });
    expect(action).toHaveBeenCalledTimes(1);
    expect(router.state.location.pathname).toBe('/pages');
    expect(screen.getAllByText('Page created successfully')).toHaveLength(1);
    router.dispose();
  });
});
