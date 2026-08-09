import React, { useEffect, useState } from 'react';
import Footer from './Footer';
import Header from './Header';
import { Outlet, useLocation } from 'react-router';
import Breadcrumbs from '@components/Breadcrumbs';
import AdminSidebar from './AdminSidebar';
import { ChannelProvider } from '../contexts/ChannelContext';
import { useLoaderData } from 'react-router';
import type { Channel } from '../models/channel';

interface AuthLoaderData {
  channels: readonly Channel[];
}

const PrimaryLayout: React.FC = () => {
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const location = useLocation();
  const { channels } = useLoaderData() as AuthLoaderData;

  useEffect(() => {
    window.scrollTo({ top: 0, left: 0, behavior: 'auto' });
  }, [location.pathname]);

  return (
    <ChannelProvider channels={channels}>
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
