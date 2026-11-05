import { Outlet, useNavigation } from 'react-router';
import TitleComponent from '@layouts/title-header';
import Footer from '@layouts/footer';
import ScrollToTop from '@utils/scroll-to-top';
import { PublicNavigationProvider } from './navigation';
import PushOptIn from '../../components/push-opt-in';
import { PublicAccountProvider } from './account';
export default function Root() {
  const navigation = useNavigation();
  return (
    <PublicNavigationProvider>
      <PublicAccountProvider>
        <>
          <ScrollToTop />
          <TitleComponent />
          <div
            id="main-content"
            tabIndex={-1}
            data-bs-theme="dark"
            className={`site-content ${navigation.state === 'loading' ? 'loading' : ''}`}
            aria-busy={navigation.state === 'loading'}
          >
            <Outlet />
          </div>
          <Footer />
          <PushOptIn />
        </>
      </PublicAccountProvider>
    </PublicNavigationProvider>
  );
}
