import { Link } from 'react-router';
import './footer.scss';
import PublicNavigationLink from '../components/PublicNavigationLink';
import { usePublicNavigation } from '../routes/layout/navigation';
import { ConsentPreferencesButton } from '../analytics/ConsentBanner';
import SocialLinks from '../components/SocialLinks';
export default function TitleComponent() {
  const { navigation, loading, error } = usePublicNavigation();

  return (
    <footer className="site-footer mt-auto" data-bs-theme="dark">
      <div className="site-footer__inner">
        <div className="row">
          <div className="col-4 col-md-3">
            <a
              href="/"
              className="d-flex align-items-center mb-3 link-body-emphasis text-decoration-none"
            >
              <img
                title="logo"
                alt="logo"
                src="/images/logo-150.png"
                style={{ height: '75px', width: '75px' }}
              />
            </a>
            <p className="mb-0 fw-bold">MorWalPiz</p>
            <p>&copy; {new Date().getFullYear()}</p>
          </div>
          {loading && (
            <div className="col-8 col-md-6" role="status" aria-label="Loading footer navigation" />
          )}
          {error && (
            <div className="col-8 col-md-6" role="status">
              Navigation unavailable
            </div>
          )}
          {!loading &&
            !error &&
            Array.from({ length: navigation?.footerColumnCount ?? 0 }, (_, column) => (
              <div className="col-4 col-md-3" key={column}>
                <h5>Menu</h5>
                <ul className="nav flex-column">
                  {navigation?.footerItems
                    .filter((item) => item.column === column)
                    .map((item) => (
                      <li
                        className="nav-item mb-2"
                        key={`${item.targetUrl}-${item.displayText}-${item.displayOrder}`}
                      >
                        <PublicNavigationLink item={item} className="nav-link p-0 link-light" />
                      </li>
                    ))}
                </ul>
              </div>
            ))}
          <div className="col-8 col-md-6">
            <SocialLinks compact />
            <div className="site-footer__destinations"><Link to="/sponsors">Sponsor</Link><Link to="/attrezzatura">Attrezzatura</Link><Link to="/blog">Articoli</Link><a href="https://www.buymeacoffee.com/MorWalPiz" target="_blank" rel="noopener noreferrer">Sostieni il canale</a></div>
            <ul className="nav flex-column">
              <li className="nav-item mb-2">
                <Link to="/privacy" className="nav-link p-0 link-light">
                  Informativa Privacy
                </Link>
              </li>
              <li className="nav-item mb-2">
                <Link to="/terms" className="nav-link p-0 link-light">
                  Termini di utilizzo
                </Link>
              </li>
              <li className="nav-item mb-2">
                <ConsentPreferencesButton />
              </li>
            </ul>
          </div>
        </div>
      </div>
    </footer>
  );
}
