import React from 'react';
import { render as rtlRender, RenderOptions } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { ToastProvider } from '@components/ToastNotification';

const customRender = (ui: React.ReactElement, options?: Omit<RenderOptions, 'wrapper'>) => {
  const router = createMemoryRouter([
    {
      path: '/',
      element: <ToastProvider>{ui}</ToastProvider>,
    },
  ]);
  return rtlRender(<RouterProvider router={router} />, options);
};

export * from '@testing-library/react';
export { customRender as render };
