import { Form, Link, NavLink, useLocation, useSearchParams } from 'react-router';
import { useEffect, useRef, useState } from 'react';
import './title-header.scss';
import PublicNavigationLink from '../components/PublicNavigationLink';
import SocialLinks from '../components/SocialLinks';
import { usePublicNavigation } from '../routes/layout/navigation';
import { usePublicAccount } from '../routes/layout/account';

interface TitleComponentProps {
  dimensions?: string;
  hideLink?: boolean;
}

export default function TitleComponent(props: TitleComponentProps) {
  const { navigation, loading, error } = usePublicNavigation();
  const [menuOpen, setMenuOpen] = useState(false);
  const menuToggleRef = useRef<HTMLButtonElement>(null);
  const navigationRef = useRef<HTMLDivElement>(null);
  const [params] = useSearchParams();
  const { pathname } = useLocation();
  const selectedType = pathname === '/' ? params.get('type') : null;

  useEffect(() => {
    if (!menuOpen) return;

    const navigationElement = navigationRef.current;
    const focusableElements = navigationElement?.querySelectorAll<HTMLElement>(
      'a, button, input, [tabindex]:not([tabindex="-1"])'
    );
    focusableElements?.[0]?.focus();

    function handleMenuKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        setMenuOpen(false);
        menuToggleRef.current?.focus();
        return;
      }

      if (event.key !== 'Tab' || !focusableElements?.length) return;
      const first = focusableElements[0];
      const last = focusableElements[focusableElements.length - 1];
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    }

    document.addEventListener('keydown', handleMenuKeyDown);
    return () => document.removeEventListener('keydown', handleMenuKeyDown);
  }, [menuOpen]);

  useEffect(() => {
    setMenuOpen(false);
  }, [pathname]);

  function closeMenu() {
    setMenuOpen(false);
    menuToggleRef.current?.focus();
  }

  return (
    <header className={`site-header ${props.dimensions ?? ''}`} data-bs-theme="dark">
      <div className="site-header__inner">
        <Link to="/" className="site-brand" aria-label="MorWalPiz, homepage">
          <img src="/images/logo-300.png" alt="" width="54" height="54" />
          <span>MorWalPiz</span>
        </Link>
        {!props.hideLink && (
          <>
            <a className="skip-link" href="#main-content">
              Vai ai contenuti
            </a>
            <button
              ref={menuToggleRef}
              type="button"
              className="icon-button site-menu-toggle"
              aria-label={menuOpen ? 'Chiudi menu' : 'Apri menu'}
              aria-expanded={menuOpen}
              aria-controls="site-navigation"
              onClick={() => (menuOpen ? closeMenu() : setMenuOpen(true))}
            >
              <i className={`fas ${menuOpen ? 'fa-times' : 'fa-bars'}`} aria-hidden="true" />
            </button>
            <div
              ref={navigationRef}
              className={`site-header__navigation${menuOpen ? ' is-open' : ''}`}
              id="site-navigation"
            >
              <nav
                aria-label="Navigazione principale"
                className="site-navigation"
                onClick={(event) => {
                  if ((event.target as HTMLElement).closest('a')) closeMenu();
                }}
              >
                <Link
                  to="/"
                  className={pathname === '/' && !selectedType ? 'is-active' : ''}
                  aria-current={pathname === '/' && !selectedType ? 'page' : undefined}
                >
                  <i className="fas fa-home" aria-hidden="true" />
                  Home
                </Link>
                <Link
                  to="/?type=video#latest-content"
                  className={selectedType === 'video' ? 'is-active' : ''}
                >
                  <i className="fas fa-play" aria-hidden="true" />
                  Video
                </Link>
                <Link
                  to="/?type=collection#latest-content"
                  className={selectedType === 'collection' ? 'is-active' : ''}
                >
                  <i className="fas fa-layer-group" aria-hidden="true" />
                  Serie
                </Link>
                <NavLink to="/blog" className={({ isActive }) => (isActive ? 'is-active' : '')}>
                  <i className="far fa-file-alt" aria-hidden="true" />
                  Articoli
                </NavLink>
                {!loading &&
                  !error &&
                  navigation?.headerItems
                    .filter((item) => !['/', '/blog'].includes(item.targetUrl))
                    .map((item) => (
                      <PublicNavigationLink
                        key={`${item.targetUrl}-${item.displayOrder}`}
                        item={item}
                        className="site-navigation__configured"
                      />
                    ))}
              </nav>
              {loading && (
                <span className="visually-hidden" role="status">
                  Caricamento menu
                </span>
              )}
              {error && (
                <span className="site-navigation__error" role="status">
                  Menu aggiuntivo non disponibile
                </span>
              )}
            </div>
            <div className="site-header__actions">
              <details className="site-search">
                <summary className="icon-button" aria-label="Cerca nei contenuti" title="Cerca">
                  <i className="fas fa-search" aria-hidden="true" />
                </summary>
                <Form method="get" action="/" role="search">
                  <label className="visually-hidden" htmlFor="site-search-input">
                    Cerca nei contenuti
                  </label>
                  <input
                    id="site-search-input"
                    name="q"
                    type="search"
                    placeholder="Cerca nei contenuti…"
                    defaultValue={params.get('q') ?? ''}
                    required
                  />
                  <button className="icon-button" aria-label="Cerca" type="submit">
                    <i className="fas fa-arrow-right" aria-hidden="true" />
                  </button>
                </Form>
              </details>
              <SocialLinks compact />
              <AccountAction />
            </div>
          </>
        )}
      </div>
    </header>
  );
}

function AccountAction() {
  const { session } = usePublicAccount();
  if (!session.authenticated)
    return (
      <Link
        to="/account/login"
        className="icon-button"
        aria-label="Accedi al tuo account"
        title="Accedi"
      >
        <i className="far fa-user" aria-hidden="true" />
      </Link>
    );
  const name = session.account?.displayName || session.account?.username || 'Profilo';
  return (
    <Link
      to="/account/profile"
      className="icon-button account-action-profile"
      aria-label={`Profilo di ${name}`}
      title={name}
    >
      <i className="far fa-user" aria-hidden="true" />
    </Link>
  );
}
