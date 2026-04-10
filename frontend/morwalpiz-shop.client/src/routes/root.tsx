import { Outlet } from 'react-router';
import { AppShell } from '@morwalpiz/layout';
import { AuthProvider } from '../contexts/AuthContext';
import { layoutConfig } from '../utils/layout-config';

export default function Root() {
  return (
    <AuthProvider>
      <AppShell config={layoutConfig}>
        <Outlet />
      </AppShell>
    </AuthProvider>
  );
}
