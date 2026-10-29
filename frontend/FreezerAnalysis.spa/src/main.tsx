import React from 'react';
import ReactDOM from 'react-dom/client';
import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import { registerSW } from 'virtual:pwa-register';
import { FreezerApiError, freezerApi, rememberedAccountId, type MatchSummary } from './freezerApi';
import { clearAccountOfflineData, getSnapshot as getOfflineSnapshot } from './offlineStore';
import {
  FieldPage,
  IdentityReviewPage,
  LoginPage,
  MatchPage,
  MatchesPage,
  ProfilePage,
  RouteErrorPage,
  RootLayout,
  VerificationPage,
  type RootData
} from './routes';
import './styles.css';

registerSW({ immediate: true });

const router = createBrowserRouter([
  {
    id: 'root',
    path: '/',
    loader: async (): Promise<RootData> => {
      const previousAccountId = rememberedAccountId();
      try {
        const session = await freezerApi.getSession();
        if (previousAccountId && session.account && previousAccountId !== session.account.id)
          await clearAccountOfflineData(previousAccountId);
        if (session.account) {
          window.sessionStorage.setItem('freezer-analysis.account-id', session.account.id);
        }
        return { session, offline: false };
      } catch (error) {
        if (!(error instanceof TypeError)) throw error;
        const accountId = rememberedAccountId();
        return {
          session: accountId
            ? { authenticated: true, account: { id: accountId, username: '', email: '', displayName: 'Modalità offline' } }
            : { authenticated: false, account: null },
          offline: true
        };
      }
    },
    element: <RootLayout />,
    errorElement: <RouteErrorPage />,
    children: [
      {
        index: true,
        loader: async () => {
          const session = await activeSession();
          if (!session.authenticated) return [];
          try {
            return await freezerApi.getMatches();
          } catch (error) {
            if (error instanceof TypeError) return [];
            throw error;
          }
        },
        element: <MatchesPage />
      },
      {
        path: 'profile',
        loader: async () => {
          const session = await activeSession();
          if (!session.authenticated) return null;
          const [profile, identities] = await Promise.all([
            freezerApi.getProfile(),
            freezerApi.getIdentities()
          ]);
          return { profile, identities };
        },
        element: <ProfilePage />
      },
      {
        path: 'matches/:matchId/identities',
        loader: async ({ params }) => {
          const matchId = params.matchId;
          if (!matchId) throw new Error('ID gara mancante.');
          if (!(await activeSession()).authenticated) return null;
          try {
            return { entries: await freezerApi.getIdentityReview(matchId), offline: false };
          } catch (error) {
            if (error instanceof TypeError) return { entries: null, offline: true };
            throw error;
          }
        },
        element: <IdentityReviewPage />
      },
      {
        path: 'matches/:matchId',
        loader: async ({ params }) => {
          const matchId = params.matchId;
          if (!matchId) throw new Error('ID gara mancante.');
          if (!(await activeSession()).authenticated) return null;
          const [draft, estimate] = await Promise.all([
          freezerApi.getDraft(matchId),
          freezerApi.getEstimate(matchId)
          ]);
          return { draft, estimate };
        },
        element: <MatchPage />
      },
      {
        path: 'field/:snapshotId/verification',
        loader: async ({ params }) => {
          const snapshotId = params.snapshotId;
          if (!snapshotId) throw new Error('ID congelamento mancante.');
          if (!(await activeSession()).authenticated) return null;
          const snapshot = await freezerApi.getSnapshot(snapshotId);
          let match: MatchSummary | null = null;
          try {
            match = await freezerApi.getMatch(snapshot.matchId);
          } catch (error) {
            if (!(error instanceof FreezerApiError) || error.status !== 404) throw error;
          }
          return { snapshot, match };
        },
        element: <VerificationPage />
      },
      {
        path: 'field/:snapshotId',
        loader: async ({ params }) => {
          const snapshotId = params.snapshotId;
          if (!snapshotId) throw new Error('ID congelamento mancante.');
          const session = await activeSession();
          if (!session.authenticated && !rememberedAccountId()) return null;
          try {
            const [snapshot, events] = await Promise.all([
              freezerApi.getSnapshot(snapshotId),
              freezerApi.getFieldEvents(snapshotId)
            ]);
            return { snapshot, events, offline: false };
          } catch (error) {
            if (!(error instanceof TypeError)) throw error;
            const accountId = rememberedAccountId();
            if (!accountId) throw error;
            const snapshot = await getOfflineSnapshot(accountId, snapshotId);
            if (!snapshot) throw error;
            return { snapshot, events: [], offline: true };
          }
        },
        element: <FieldPage />
      },
      { path: 'login', element: <LoginPage /> }
    ]
  }
]);

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <RouterProvider router={router} />
  </React.StrictMode>
);

async function activeSession() {
  try {
    return await freezerApi.getSession();
  } catch (error) {
    if (!(error instanceof TypeError)) throw error;
    return { authenticated: rememberedAccountId() !== null, account: null };
  }
}
