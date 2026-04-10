# @morwalpiz/layout

Reusable layout components for MorWalPiz applications.

## Installation

This package is part of the MorWalPiz monorepo and should be installed via workspace references.

```json
{
  "dependencies": {
    "@morwalpiz/layout": "file:../fe-packages/layout/morwalpiz-layout"
  }
}
```

## Usage

### Basic Setup

```tsx
import { AppShell, LayoutConfig } from '@morwalpiz/layout';
import '@morwalpiz/layout/styles';

const layoutConfig: LayoutConfig = {
  brand: {
    name: 'My App',
    logo: '/images/logo.png',
    homeRoute: '/',
  },
  header: {
    navigation: [
      { label: 'Home', path: '/' },
      { label: 'About', path: '/about' },
    ],
    socialLinks: [
      { platform: 'telegram', url: 'https://t.me/myapp', label: 'Join' },
    ],
  },
  footer: {
    sections: [
      {
        title: 'Pages',
        links: [
          { label: 'Home', path: '/' },
          { label: 'About', path: '/about' },
        ],
      },
    ],
    copyright: `© ${new Date().getFullYear()} My App`,
  },
};

function Root() {
  return (
    <AppShell config={layoutConfig}>
      <Outlet />
    </AppShell>
  );
}
```

## Components

### AppShell

The main layout wrapper component.

**Props:**
- `config: LayoutConfig` - Layout configuration object
- `children: ReactNode` - Page content
- `loading?: boolean` - Optional loading state

### SiteHeader

Parametric header component with brand, navigation, and social links.

### SiteFooter

Parametric footer component with sections and legal links.

## Configuration

See `types.ts` for full TypeScript interfaces.

## Styles

Import the compiled CSS:

```tsx
import '@morwalpiz/layout/styles';
```

## Building

```bash
npm run build
```

Built files are output to `dist/`.