import { Outlet, useLocation, useNavigation } from 'react-router';
import TitleComponent from '@layouts/title-header';
import Footer from '@layouts/footer';
import ScrollToTop from '@utils/scroll-to-top';
import { PublicNavigationProvider } from './navigation';
import PushOptIn from '../../components/push-opt-in';
import { PublicAccountProvider } from './account';
export default function Root() {
  const navigation = useNavigation();
  const isHome = useLocation().pathname === '/';
  return (
    <PublicNavigationProvider>
      <PublicAccountProvider>
        <>
          <ScrollToTop />
          <TitleComponent />
          <main id="main-content" data-bs-theme={isHome ? 'dark' : 'light'} className={`site-content ${isHome ? 'site-content--home' : 'site-content--legacy container my-4'} ${navigation.state === 'loading' ? 'loading' : ''}`} aria-busy={navigation.state === 'loading'}>
            <Outlet />
          </main>
          <Footer />
          <PushOptIn />
        </>
      </PublicAccountProvider>
    </PublicNavigationProvider>
  );
}
