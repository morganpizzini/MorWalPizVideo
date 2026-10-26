import { Link } from 'react-router';
import { useState } from 'react';
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
  const { session, loading: accountLoading, logout } = usePublicAccount();
  const [logoutError, setLogoutError] = useState('');

  const handleLogout = async () => {
    setLogoutError('');
    try {
      await logout();
    } catch {
      setLogoutError('Non è stato possibile uscire. Riprova.');
    }
  };

  return (
    <div className={`container text-center title-header ${props.dimensions}`}>
      {!props.hideLink && (
        <div className="account-actions" aria-label="Account">
          {accountLoading ? (
            <span className="account-actions-placeholder" aria-hidden="true" />
          ) : session.authenticated ? (
            <>
              <Link to="/account/profile" className="account-action account-action-profile">
                <i className="fas fa-user" aria-hidden="true" />
                <span>Profilo</span>
              </Link>
              <button
                type="button"
                className="account-action account-action-logout"
                onClick={handleLogout}
              >
                <i className="fas fa-sign-out-alt" aria-hidden="true" />
                <span>Esci</span>
              </button>
            </>
          ) : (
            <>
              <Link to="/account/login" className="account-action account-action-login">
                <i className="fas fa-sign-in-alt" aria-hidden="true" />
                <span>Accedi</span>
              </Link>
              <Link to="/account/register" className="account-action account-action-register">
                <i className="fas fa-user-plus" aria-hidden="true" />
                <span>Registrati</span>
              </Link>
            </>
          )}
          {logoutError && (
            <span className="account-action-error" role="alert">
              {logoutError}
            </span>
          )}
        </div>
      )}
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
            </nav>
          ) : (
            <nav
              aria-label="Main navigation"
              className="d-flex justify-content-center gap-3 mt-3"
            />
          )}
        </>
      )}
    </div>
  );
}
