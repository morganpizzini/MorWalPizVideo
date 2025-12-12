import React from 'react';
import Footer from './Footer';
import Header from './Header';
import { Outlet } from 'react-router';
import Breadcrumbs from '@components/Breadcrumbs';

const PrimaryLayout: React.FC = () => {
  return (
    <>
      <Header />
      <div className="container mt-4">
        <Breadcrumbs />
        <Outlet />
      </div>
      <Footer />
    </>
  );
};

export default PrimaryLayout;
