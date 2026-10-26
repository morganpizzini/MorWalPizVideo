import { Link } from 'react-router';
import './title-header.scss';
import PublicNavigationLink from '../components/PublicNavigationLink';
import { usePublicNavigation } from '../routes/layout/navigation';
import { usePublicAccount } from '../routes/layout/account';

interface TitleComponentProps {
  dimensions?: string;
  hideLink?: boolean;
}

export default function TitleComponent(props: TitleComponentProps) {
  const { navigation, loading, error } = usePublicNavigation();
  const { session } = usePublicAccount();

  return (
    <div className={`container text-center ${props.dimensions}`}>
      <div className="title-container">
        <Link to={``}>
          <h1 className="title">
            <span className="big-letter">M</span>
            <span className="small-letter">or</span>
            <span className="big-letter">W</span>
            <span className="small-letter">al</span>
            <span className="big-letter">P</span>
            <span className="small-letter">iz</span>
          </h1>
        </Link>
      </div>
      {!props.hideLink && (
        <>
          <div className="social-buttons mt-3">
            <a
              href="https://t.me/morwalpiz"
              target="_blank"
              rel="noopener noreferrer"
              className="btn btn-telegram"
            >
              <i className="fab fa-telegram"></i> Aggiungi
            </a>
            <a
              href="https://www.youtube.com/@morwalpiz?sub_confirmation=1"
              target="_blank"
              rel="noopener noreferrer"
              className="btn btn-youtube"
            >
              <i className="fab fa-youtube"></i> Iscriviti
            </a>
            <a
              href="https://www.instagram.com/morwalpiz"
              target="_blank"
              rel="noopener noreferrer"
              className="btn btn-instagram"
            >
              <i className="fab fa-instagram"></i> Seguimi
            </a>
          </div>
        </>
      )}
      {!props.hideLink && (
        <>
          {loading && (
            <div
              className="public-navigation-loading"
              role="status"
              aria-label="Loading navigation"
            />
          )}
          {error && (
            <div className="alert alert-light py-1 mt-3" role="status">
              Navigation unavailable
            </div>
          )}
          {!loading && !error && navigation?.headerItems.length ? (
            <nav
              aria-label="Main navigation"
              className="d-flex flex-wrap justify-content-center gap-3 mt-3"
            >
              {navigation.headerItems.map((item) => (
                <PublicNavigationLink
                  key={`${item.targetUrl}-${item.displayText}-${item.displayOrder}`}
                  item={item}
                  className="nav-link"
                />
              ))}
              <Link
                to={session.authenticated ? '/account/profile' : '/account/login'}
                className="nav-link"
              >
                {session.authenticated ? 'Profilo' : 'Accedi'}
              </Link>
            </nav>
          ) : (
            <nav aria-label="Account navigation" className="d-flex justify-content-center mt-3">
              <Link
                to={session.authenticated ? '/account/profile' : '/account/login'}
                className="nav-link"
              >
                {session.authenticated ? 'Profilo' : 'Accedi'}
              </Link>
            </nav>
          )}
        </>
      )}
    </div>
  );
}
