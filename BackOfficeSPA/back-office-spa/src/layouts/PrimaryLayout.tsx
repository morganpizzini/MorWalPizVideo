import React, { useState } from 'react';
import Footer from './Footer';
import Header from './Header';
import Sidebar from './Sidebar';
import { Outlet, useLocation } from 'react-router';
import Breadcrumbs from '@components/Breadcrumbs';

const PrimaryLayout: React.FC = () => {
  const [showSidebar, setShowSidebar] = useState(false);
  const location = useLocation();

  const toggleSidebar = () => {
    // Only toggle sidebar if we're not on the homepage
    if (location.pathname !== '/') {
      setShowSidebar(!showSidebar);
    }
  };

  const handleCloseSidebar = () => {
    setShowSidebar(false);
  };

  return (
    <>
      <Header />
      <Sidebar show={showSidebar} handleClose={handleCloseSidebar} />
      <div className="container mt-4">
        <Breadcrumbs toggleSidebar={toggleSidebar} />
        <Outlet />
      </div>
      <Footer />
    </>
  );
};

export default PrimaryLayout;
