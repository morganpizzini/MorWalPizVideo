import { ReactNode } from 'react';
import { useNavigation } from 'react-router';
import type { LayoutConfig } from '../types.js';
import { SiteHeader } from './SiteHeader.js';
import { SiteFooter } from './SiteFooter.js';

export interface AppShellProps {
  config: LayoutConfig;
  children: ReactNode;
  loading?: boolean;
}

export function AppShell({ config, children, loading }: AppShellProps) {
  const navigation = useNavigation();
  const isLoading = loading || navigation.state === 'loading';

  return (
    <div className="app-shell">
      <SiteHeader config={config.header} brand={config.brand} />
      
      <main className={`container my-5 ${isLoading ? 'loading' : ''}`}>
        {children}
      </main>
      
      <SiteFooter config={config.footer} brand={config.brand} />
    </div>
  );
}