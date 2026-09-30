import React, { useEffect, useState } from 'react';
import Footer from './Footer';
import Header from './Header';
import { Outlet, useLocation, useNavigation } from 'react-router';
import Breadcrumbs from '@components/Breadcrumbs';
import AdminSidebar from './AdminSidebar';
import { ChannelProvider } from '../contexts/ChannelContext';
import { useLoaderData } from 'react-router';
import type { Channel } from '../models/channel';
import {
  DeferredRouteProgressProvider,
  useDeferredRoutePending,
} from '../contexts/DeferredRouteProgressContext';

interface AuthLoaderData {
  channels: readonly Channel[];
}

const PrimaryLayout: React.FC = () => {
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const location = useLocation();
  const navigation = useNavigation();
  const { channels } = useLoaderData() as AuthLoaderData;

  useEffect(() => {
    window.scrollTo({ top: 0, left: 0, behavior: 'auto' });
  }, [location.pathname]);

  return (
    <DeferredRouteProgressProvider>
      <PrimaryLayoutContent
        channels={channels}
        navigationState={navigation.state}
        sidebarOpen={sidebarOpen}
        setSidebarOpen={setSidebarOpen}
      />
    </DeferredRouteProgressProvider>
  );
};

interface PrimaryLayoutContentProps {
  channels: readonly Channel[];
  navigationState: ReturnType<typeof useNavigation>['state'];
  sidebarOpen: boolean;
  setSidebarOpen: React.Dispatch<React.SetStateAction<boolean>>;
}

const PrimaryLayoutContent: React.FC<PrimaryLayoutContentProps> = ({
  channels,
  navigationState,
  sidebarOpen,
  setSidebarOpen,
}) => {
  const deferredPending = useDeferredRoutePending();

  return (
    <ChannelProvider channels={channels}>
      <div
        className={`router-progress ${navigationState === 'idle' && !deferredPending ? '' : 'is-active'}`}
        role="status"
        aria-live="polite"
        aria-label={navigationState === 'submitting' ? 'Saving changes' : 'Loading page'}
      />
      <div className="admin-shell">
        <AdminSidebar show={sidebarOpen} onHide={() => setSidebarOpen(false)} />
        <div className="admin-main admin-main-layout">
          <Header onToggleSidebar={() => setSidebarOpen(open => !open)} />
          <main className="admin-content">
            <Breadcrumbs />
            <Outlet />
          </main>
          <Footer />
        </div>
      </div>
    </ChannelProvider>
  );
};

export default PrimaryLayout;
