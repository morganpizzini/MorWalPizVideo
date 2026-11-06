import {
  useState
} from "react";
import {
  Link,
  NavLink,
  Outlet,
  useLocation,
  useRevalidator,
  useRouteError,
  isRouteErrorResponse,
  useMatches
} from "react-router-dom";


import {
  clearRememberedAccountId,
  freezerApi
} from "../freezerApi";
import {
  clearAccountOfflineData
} from "../offlineStore";

import {
  rootData,
  ErrorMessage,
  errorMessage
} from "./shared";

export function RouteErrorPage() {
  const error = useRouteError();
  const message = isRouteErrorResponse(error)
    ? `${error.status}: ${error.statusText}`
    : error instanceof Error
      ? error.message
      : "La pagina non è disponibile.";
  return (
    <main className="page-frame">
      <section className="auth-card" role="alert">
        <h1>Questa pagina non è pronta.</h1>
        <p className="lede">{message}</p>
        <Link className="button button-primary" to="/">
          Torna alle gare
        </Link>
      </section>
    </main>
  );
}

export function RootLayout() {
  const { session, offline } = rootData();
  const location = useLocation();
  const activeSnapshotId = useMatches()
    .map((match) => match.params.snapshotId)
    .find((value): value is string => Boolean(value));
  const revalidator = useRevalidator();
  const [logoutError, setLogoutError] = useState("");
  const account = session.account;

  async function logout() {
    setLogoutError("");
    try {
      if (account) await clearAccountOfflineData(account.id);
      await freezerApi.logout();
      clearRememberedAccountId();
      revalidator.revalidate();
    } catch (error) {
      setLogoutError(errorMessage(error));
    }
  }

  return (
    <div className="app-shell">
      <header className="topbar">
        <Link className="brand" to="/" aria-label="FreezerAnalysis — gare">
          <span className="brand-mark" aria-hidden="true">
            F
          </span>
          <span>
            Freezer<span className="brand-light">Analysis</span>
          </span>
        </Link>
        <nav className="main-nav" aria-label="Navigazione principale">
          {account && <>
            <NavLink to="/" end>
              Gare
            </NavLink>
            <NavLink to="/profile">Profilo</NavLink>
          </>}
        </nav>
        <div className="account-area">
          {offline && <span className="offline-pill">Offline</span>}
          {account ? (
            <>
              <span className="account-name">
                {account.displayName || account.username}
              </span>
              {!offline && (
                <button
                  className="button button-quiet"
                  onClick={() => void logout()}
                >
                  Esci
                </button>
              )}
            </>
          ) : (
            <Link className="button button-quiet" to="/login">
              Accedi
            </Link>
          )}
        </div>
      </header>
      {logoutError && (
        <div className="shell-message">
          <ErrorMessage>{logoutError}</ErrorMessage>
        </div>
      )}
      <main className="page-frame">
        {!account && !offline && location.pathname !== "/login" ? (
          <LoginPrompt />
        ) : (
          <Outlet />
        )}
      </main>
      {account && (
        <nav className="mobile-nav" aria-label="Navigazione mobile">
          <NavLink to="/" end>
            Gare
          </NavLink>
          <NavLink to="/profile">Profilo</NavLink>
          {activeSnapshotId && (
            <NavLink to={`/field/${encodeURIComponent(activeSnapshotId)}`}>
              Campo
            </NavLink>
          )}
        </nav>
      )}
      <footer className="site-footer">
        <span>FreezerAnalysis</span>
        <span>Preparazione ragionata. Decisioni sempre tue.</span>
      </footer>
    </div>
  );
}

function LoginPrompt() {
  return (
    <section className="welcome-panel">
      <h1>La gara, letta con lucidità.</h1>
      <p>
        Accedi per importare risultati, preparare il piano e portare le note con
        te sul campo.
      </p>
      <Link to="/login" className="button button-primary">
        Accedi o crea un account
      </Link>
    </section>
  );
}

