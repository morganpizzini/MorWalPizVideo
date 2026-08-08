import React, { useState } from 'react';
import Footer from './Footer';
import Header from './Header';
import { Outlet } from 'react-router';
import Breadcrumbs from '@components/Breadcrumbs';
import AdminSidebar from './AdminSidebar';

const PrimaryLayout: React.FC = () => {
  const [sidebarOpen, setSidebarOpen] = useState(false);

  return (
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
  );
};

export default PrimaryLayout;
